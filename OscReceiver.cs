using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TotalMixKeyControl
{
	internal sealed class OscReceiver : IDisposable
	{
		private UdpClient? _udp;
		private CancellationTokenSource? _cts;
		private Task? _receiveTask;

		public event Action<string, float>? FloatMessageReceived;
		public event Action<string, string>? StringMessageReceived;

		public int Port { get; private set; }

		public void Start(int port)
		{
			Stop();
			Port = port;
			_udp = new UdpClient(new IPEndPoint(IPAddress.Any, port));
			_cts = new CancellationTokenSource();
			_receiveTask = Task.Run(() => LoopAsync(_cts.Token));
		}

		public void Stop()
		{
			_cts?.Cancel();
			try { _udp?.Close(); }
			catch (Exception exception) { Log.Error("Failed to close OSC UDP socket.", exception); }
			try { _udp?.Dispose(); }
			catch (Exception exception) { Log.Error("Failed to dispose OSC UDP socket.", exception); }
			_udp = null;
			if (_receiveTask != null)
			{
				try { _receiveTask.Wait(250); }
				catch (Exception exception) { Log.Error("Failed to stop OSC receiver task.", exception); }
				_receiveTask = null;
			}
			_cts?.Dispose();
			_cts = null;
		}

		private async Task LoopAsync(CancellationToken token)
		{
			var udp = _udp;
			if (udp == null) return;
			while (!token.IsCancellationRequested)
			{
				try
				{
					var result = await udp.ReceiveAsync(token);
					var data = result.Buffer;
					if (data == null || data.Length < 8) continue;
					ParseOscPacket(data);
				}
				catch (OperationCanceledException)
				{
					break;
				}
				catch (ObjectDisposedException)
				{
					break;
				}
				catch (SocketException exception)
				{
					if (token.IsCancellationRequested) break;
					Log.Error("OSC socket error.", exception);
				}
				catch (Exception exception)
				{
					Log.Error("OSC receive loop error.", exception);
				}
			}
		}

		private void ParseOscPacket(byte[] buffer)
		{
			int idx = 0;
			string address = ReadOscString(buffer, ref idx);
			if (string.IsNullOrEmpty(address)) return;

			if (string.Equals(address, "#bundle", StringComparison.Ordinal))
			{
				if (idx + 8 > buffer.Length) return;
				idx += 8;
				while (idx + 4 <= buffer.Length)
				{
					int elemLen = ReadInt32BE(buffer, ref idx);
					if (elemLen <= 0 || idx + elemLen > buffer.Length) break;
					var elem = new byte[elemLen];
					Buffer.BlockCopy(buffer, idx, elem, 0, elemLen);
					idx += elemLen;
					try { ParseOscPacket(elem); }
					catch (Exception exception)
					{
						Log.Error("Failed to parse OSC bundle element.", exception);
					}
				}
				return;
			}

			string types = ReadOscString(buffer, ref idx);
			if (string.IsNullOrEmpty(types) || types[0] != ',') return;

			if (types == ",f" || (types.Length > 1 && types[1] == 'f'))
			{
				if (idx + 4 <= buffer.Length)
				{
					float f = ReadOscFloat(buffer, ref idx);
					FloatMessageReceived?.Invoke(address, f);
				}
			}
			else if (types == ",s")
			{
				string s = ReadOscString(buffer, ref idx);
				StringMessageReceived?.Invoke(address, s);
			}
		}

		private static string ReadOscString(byte[] buffer, ref int idx)
		{
			int start = idx;
			while (idx < buffer.Length && buffer[idx] != 0) idx++;
			if (idx >= buffer.Length) return string.Empty;
			string s = Encoding.UTF8.GetString(buffer, start, idx - start);
			idx++;
			while (idx % 4 != 0 && idx < buffer.Length) idx++;
			return s;
		}

		private static float ReadOscFloat(byte[] buffer, ref int idx)
		{
			var bytes = new byte[4];
			Buffer.BlockCopy(buffer, idx, bytes, 0, 4);
			idx += 4;
			if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
			return BitConverter.ToSingle(bytes, 0);
		}

		private static int ReadInt32BE(byte[] buffer, ref int idx)
		{
			int val = (buffer[idx] << 24) | (buffer[idx + 1] << 16) | (buffer[idx + 2] << 8) | buffer[idx + 3];
			idx += 4;
			return val;
		}

		public void Dispose()
		{
			Stop();
		}
	}
}
