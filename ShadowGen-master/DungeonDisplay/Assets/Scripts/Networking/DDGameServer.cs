using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using Steamworks.Data;
using System;
using System.Threading.Tasks;
using UnityEngine.Assertions;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

public class DDGameServer : SocketManager
{
	public override void OnConnecting(Connection connection, ConnectionInfo data)
	{
		connection.Accept();
		InfoLogCanvasScript.SendInfoMessage($"Client is connecting", UnityEngine.Color.yellow);
		base.OnConnecting(connection, data);
	}

	public override void OnConnected(Connection connection, ConnectionInfo data)
	{
		InfoLogCanvasScript.SendInfoMessage($"Client has joined the game", UnityEngine.Color.yellow);
		base.OnConnected(connection, data);

		if(Connected.Count > 1)
        {
			InfoLogCanvasScript.SendInfoMessage($"{Connected.Count}", UnityEngine.Color.yellow);
			try
			{
				//Send the connecter to the main menu
				InfoLogCanvasScript.SendInfoMessage($"Client return to main menu", UnityEngine.Color.yellow);
				Connected[Connected.Count - 1].SendMessage(Encoding.UTF8.GetBytes("rms"));

				//Send the connector the required image files
				InfoLogCanvasScript.SendInfoMessage($"Client retrieving file data", UnityEngine.Color.yellow);
				List<byte[]> imageData = Manager.Instance.RetrieveMapImageData();
				foreach (byte[] b in imageData)
				{
					Connected[Connected.Count - 1].SendMessage(b);
				}

				InfoLogCanvasScript.SendInfoMessage($"Client retrieving file locations", UnityEngine.Color.yellow);
				//Tell the connector to add the image files
				Connected[Connected.Count - 1].SendMessage(Encoding.UTF8.GetBytes($"did{Manager.Instance.RetrieveMapImageLocations()}"));

				//Send the connecter a save file
				InfoLogCanvasScript.SendInfoMessage($"Client retrieving map file", UnityEngine.Color.yellow);
				Manager.Instance.SaveMap("ServerMapSaveTemp");

				//Send the map file then its location
				Connected[Connected.Count - 1].SendMessage(File.ReadAllBytes(File.ReadAllLines(@"GameLoadedAssets\Maps\SavedMaps\ServerMapSaveTemp.txt")[0]));
				Connected[Connected.Count - 1].SendMessage(Encoding.UTF8.GetBytes($"dmd{File.ReadAllLines(@"GameLoadedAssets\Maps\SavedMaps\ServerMapSaveTemp.txt")[0]}"));

				Connected[Connected.Count - 1].SendMessage(File.ReadAllBytes(@"GameLoadedAssets\Maps\SavedMaps\ServerMapSaveTemp.txt"));
				Connected[Connected.Count - 1].SendMessage(Encoding.UTF8.GetBytes($"lmp"));
				InfoLogCanvasScript.SendInfoMessage($"Server Send Complete", UnityEngine.Color.yellow);
			}
			catch
            {
				InfoLogCanvasScript.SendInfoMessage($"Error with server data send", UnityEngine.Color.red);
			}
		}
	}

	public override void OnDisconnected(Connection connection, ConnectionInfo data)
	{
		InfoLogCanvasScript.SendInfoMessage($"Client has left", UnityEngine.Color.yellow);
		base.OnDisconnected(connection, data);
	}

	public override unsafe void OnMessage(Connection connection, NetIdentity identity, IntPtr data, int size, long messageNum, long recvTime, int channel)
	{
		//Data Identifiers:

		//sct - going to send a tile sprite change to the clients
		//idr - image data request tile
		//ids - image data send tile

		//sco - going to send a tile sprite change to the clients
		//ior - image data request object
		//ios - image data send object

		//tor - tile set image data request
		//tos - tile set image data send

		string s = Encoding.UTF8.GetString((byte*)data, size);
		string id = s.Substring(0, 3);
		string info = s.Substring(3);

		if (id == "sct" || id == "sco" || id == "tsc") //done
		{
			foreach (Connection c in Connected)
			{
				c.SendMessage(Encoding.UTF8.GetBytes(s + $"|{Connected.IndexOf(connection)}"));
			}
		}
		else if (id == "idr") //done
		{
			Connected[int.Parse(info)].SendMessage($"fdr{Connected.IndexOf(connection)}");
		}
		else if (id == "ids") //done
		{
			string[] splitInfo = info.Split(new char[1] { '|' });

			byte[] byteLengthArray = Encoding.UTF8.GetBytes("ids" + splitInfo[0] + "|" + splitInfo[1] + "|" + splitInfo[2] + "|");
			int start = byteLengthArray.Length;
			byte[] imageDataT = new byte[size];
			Marshal.Copy(data, imageDataT, 0, size);

			List<byte> imageDataL = imageDataT.ToList();

			byte[] imageData = new byte[size - start];
			imageDataL.RemoveRange(0, start);
			imageData = imageDataL.ToArray();

			Connected[int.Parse(splitInfo[0])].SendMessage(Encoding.UTF8.GetBytes($"cpt{splitInfo[1]}|{splitInfo[2]}|").Concat(imageData).ToArray());
		}
		else if (id == "ior") //done
		{
			Connected[int.Parse(info)].SendMessage($"for{Connected.IndexOf(connection)}");
		}
		else if (id == "ios") //done
		{
			string[] splitInfo = info.Split(new char[1] { '|' });

			byte[] byteLengthArray = Encoding.UTF8.GetBytes("ios" + splitInfo[0] + "|" + splitInfo[1] + "|" + splitInfo[2] + "|");
			int start = byteLengthArray.Length;
			byte[] imageDataT = new byte[size];
			Marshal.Copy(data, imageDataT, 0, size);

			List<byte> imageDataL = imageDataT.ToList();

			byte[] imageData = new byte[size - start];
			imageDataL.RemoveRange(0, start);
			imageData = imageDataL.ToArray();

			Connected[int.Parse(splitInfo[0])].SendMessage(Encoding.UTF8.GetBytes($"cpo{splitInfo[1]}|{splitInfo[2]}|").Concat(imageData).ToArray());
		}
		else if (id == "tor") //done
		{
			string[] splitInfo = info.Split(new char[1] { '|' });
			Connected[int.Parse(splitInfo[0])].SendMessage($"fts{Connected.IndexOf(connection)}|{splitInfo[1]}");
		}
		else if (id == "tos") //done
		{
			string[] splitInfo = info.Split(new char[1] { '|' });

			byte[] byteLengthArray = Encoding.UTF8.GetBytes("tos" + splitInfo[0] + "|" + splitInfo[1] + "|" + splitInfo[2] + "|" + splitInfo[3] + "|");
			int start = byteLengthArray.Length;
			byte[] imageDataT = new byte[size];
			Marshal.Copy(data, imageDataT, 0, size);

			List<byte> imageDataL = imageDataT.ToList();

			//byte[] imageData = new byte[size - start];
			imageDataL.RemoveRange(0, start);
			byte[] imageData = imageDataL.ToArray();

			Connected[int.Parse(splitInfo[0])].SendMessage(Encoding.UTF8.GetBytes($"itl{splitInfo[1]}|{splitInfo[2]}|{splitInfo[3]}|").Concat(imageData).ToArray());
		}
		else
		{
			foreach (Connection c in Connected)
			{
				c.SendMessage(data, size);
			}
		}
	}

	internal async Task RunAsync()
	{
		while (true)
		{
			if(Connected.Count != 0)
            {
				InfoLogCanvasScript.SendInfoMessage("Server Started", UnityEngine.Color.green);
			}
			while (Connected.Count != 0)
			{
				await Task.Delay(10);
				Receive();
			}
			await Task.Delay(50);
			Receive();
		}
	}
}
