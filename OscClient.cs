using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TotalMixKeyControl
{
	internal sealed class OscClient : IDisposable
	{
		private UdpClient? _client;
		private IPEndPoint? _endpoint;

		public void Configure(string ip, int port)
		{
			DisposeClient();
			_client = new UdpClient();
			_endpoint = new IPEndPoint(IPAddress.Parse(ip), port);
		}

		public void SendFloat(string address, float value)
		{
			if (_client == null || _endpoint == null)
				throw new InvalidOperationException("OSC client not configured");

			byte[] addr = EncodeOscString(address);
			byte[] types = EncodeOscString(",f");
			byte[] val = EncodeOscFloat(value);

			var buffer = new byte[addr.Length + types.Length + val.Length];
			Buffer.BlockCopy(addr, 0, buffer, 0, addr.Length);
			Buffer.BlockCopy(types, 0, buffer, addr.Length, types.Length);
			Buffer.BlockCopy(val, 0, buffer, addr.Length + types.Length, val.Length);

			_client.Send(buffer, buffer.Length, _endpoint);
		}

		private static byte[] EncodeOscString(string s)
		{
			var bytes = Encoding.UTF8.GetBytes(s);
			int padLen = 4 - ((bytes.Length + 1) % 4);
			if (padLen == 4) padLen = 0;
			var result = new byte[bytes.Length + 1 + padLen];
			Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length);
			return result;
		}

		private static byte[] EncodeOscFloat(float f)
		{
			var bytes = BitConverter.GetBytes(f);
			if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
			return bytes;
		}

		private void DisposeClient()
		{
			try { _client?.Dispose(); } catch { }
			_client = null;
			_endpoint = null;
		}

		public void Dispose()
		{
			DisposeClient();
		}
	}
}
