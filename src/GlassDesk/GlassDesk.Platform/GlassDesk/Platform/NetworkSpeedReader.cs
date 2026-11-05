using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace GlassDesk.Platform;

public sealed class NetworkSpeedReader
{
	private readonly object _gate = new object();

	private NetworkTrafficCounter? _previous;

	private TimeSpan _previousAt;

	public NetworkSpeedSnapshot Read()
	{
		NetworkTrafficCounter current = null;
		try
		{
			bool flag = GetBestInterface(16843009u, out var routeIndex) == 0;
			NetworkInterface[] source = (from adapter in NetworkInterface.GetAllNetworkInterfaces()
				where adapter.OperationalStatus == OperationalStatus.Up && adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback
				select adapter).ToArray();
			NetworkInterface networkInterface = (flag ? source.FirstOrDefault((NetworkInterface candidate) =>
			{
				if (candidate.Supports(NetworkInterfaceComponent.IPv4))
				{
					IPv4InterfaceProperties iPv4Properties = candidate.GetIPProperties().GetIPv4Properties();
					if (iPv4Properties == null)
					{
						return false;
					}
					return iPv4Properties.Index == routeIndex;
				}
				return false;
			}) : null);
			if (networkInterface == null)
			{
				networkInterface = source.FirstOrDefault((NetworkInterface candidate) => candidate.GetIPProperties().GatewayAddresses.Any((GatewayIPAddressInformation gateway) => !gateway.Address.Equals(IPAddress.Any) && !gateway.Address.Equals(IPAddress.IPv6Any)));
			}
			if (networkInterface != null)
			{
				IPInterfaceStatistics iPStatistics = networkInterface.GetIPStatistics();
				current = new NetworkTrafficCounter(networkInterface.Id, networkInterface.Name, iPStatistics.BytesReceived, iPStatistics.BytesSent);
			}
		}
		catch (Exception ex) when ((ex is NetworkInformationException || ex is PlatformNotSupportedException) ? true : false)
		{
		}
		return Sample(current, TimeSpan.FromSeconds((double)Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency));
	}

	public void Reset()
	{
		lock (_gate)
		{
			_previous = null;
		}
	}

	public NetworkSpeedSnapshot Sample(NetworkTrafficCounter? current, TimeSpan now)
	{
		lock (_gate)
		{
			NetworkTrafficCounter previous = _previous;
			double totalSeconds = (now - _previousAt).TotalSeconds;
			_previous = current;
			_previousAt = now;
			if ((object)current == null)
			{
				return new NetworkSpeedSnapshot(Connected: false, string.Empty, null, null);
			}
			if ((object)previous == null || previous.Id != current.Id || totalSeconds <= 0.0 || totalSeconds > 5.0 || current.ReceivedBytes < previous.ReceivedBytes || current.SentBytes < previous.SentBytes)
			{
				return new NetworkSpeedSnapshot(Connected: true, current.Name, null, null);
			}
			return new NetworkSpeedSnapshot(Connected: true, current.Name, (double)(current.ReceivedBytes - previous.ReceivedBytes) / totalSeconds, (double)(current.SentBytes - previous.SentBytes) / totalSeconds);
		}
	}

	public static string FormatRate(double? bytesPerSecond)
	{
		if (bytesPerSecond.HasValue)
		{
			double num = bytesPerSecond.GetValueOrDefault();
			if (double.IsFinite(num) && !(num < 0.0))
			{
				string[] array = new string[5] { "B/s", "KB/s", "MB/s", "GB/s", "TB/s" };
				int num2 = 0;
				while (num >= 1024.0 && num2 < array.Length - 1)
				{
					num /= 1024.0;
					num2++;
				}
				return num.ToString((num2 == 0) ? "0" : "0.0", CultureInfo.CurrentCulture) + " " + array[num2];
			}
		}
		return "--";
	}

	[DllImport("iphlpapi.dll")]
	private static extern int GetBestInterface(uint destinationAddress, out int interfaceIndex);
}
