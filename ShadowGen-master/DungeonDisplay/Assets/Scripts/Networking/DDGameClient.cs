using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using Steamworks.Data;
using System;
using System.Threading.Tasks;
using UnityEngine.Assertions;
using System.Drawing;
using System.Text;
using System.IO;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Rendering.Universal;
using System.Linq;

public class DDGameClient : ConnectionManager
{
	public bool serverConnected = false;
	private bool preDisconnect = true;

	private List<byte[]> textureDataCache;

	private Vector2Int editTile = Vector2Int.zero;
	private Vector2 editObject = Vector2.zero;
	private string editPath = "";
	private int missingFileCount = 0;

	public override void OnConnecting(ConnectionInfo data)
	{
		InfoLogCanvasScript.SendInfoMessage($"Attempting connection to Server", UnityEngine.Color.blue);
		preDisconnect = true;
		base.OnConnecting(data);
	}

	public override void OnConnected(ConnectionInfo data)
	{
		InfoLogCanvasScript.SendInfoMessage($"Connected to Server", UnityEngine.Color.blue);
		serverConnected = true;
		base.OnConnected(data);
	}

	public override void OnDisconnected(ConnectionInfo data)
	{
		InfoLogCanvasScript.SendInfoMessage($"No connection to Server", UnityEngine.Color.blue);
		serverConnected = false;
		preDisconnect = false;
		base.OnDisconnected(data);
	}

	public override unsafe void OnMessage(IntPtr data, int size, long messageNum, long recvTime, int channel)
	{
		//Data Identifiers:

		//msg - info log message - done

		//lcr - create light - done
		//ocr - create object - done
		//odl - delete object or light - done
		//ovc - object value changes - done
		//opc - object position changed - done

		//cpo - check for a sprite path and change the sent object if found, else request image data - done
		//sco - sprite change light / object using sent image data - done

		//cpt - check for a sprite path and change the sent tile if found, else request image data - done
		//sct - sprite change tile using sent image data - done

		//fdr - file data request tile - done
		//for - file data request object - done
		//fts - file data request tile set - done

		//doc - door open or close - done

		//fvc - freeform light area value change - done
		//fcr - freeform light area created - done
		//svc - shadow area value change - done
		//scr - shadow area created - done

		//glt - global lighting toggled - done
		//rtm - return to main menu - done

		//tsc - tile set change - done
		//itl - individual tile set tile load - done

		//did - download image data - done
		//dmd - download map data - done
		//lmp - load map - done

		string s = Encoding.UTF8.GetString((byte*)data, size);
		string id = s.Substring(0,3);
		string info = s.Substring(3);

		if(id == "msg") //done
        {
			InfoLogCanvasScript.SendInfoMessage(info, UnityEngine.Color.black, 9, false);
		}
		else if (id == "lcr") //done
		{
			//InfoLogCanvasScript.SendInfoMessage("Light Created" + " - Sent From Server", UnityEngine.Color.black, 9, false);
			Manager.Instance.mainCanvas.ServerMakePrefab(new Vector3(float.Parse(info.Split(new char[1] { '|' })[0]), float.Parse(info.Split(new char[1] { '|' })[1]), 0));
		}
		else if (id == "odl") //done
		{
			//InfoLogCanvasScript.SendInfoMessage("Light Deleted" + " - Sent From Server", UnityEngine.Color.black, 9, false);
			Vector3 pos = new Vector3(float.Parse(info.Split(new char[1] { '|' })[0]), float.Parse(info.Split(new char[1] { '|' })[1]), 0);

			MoveObject[] cls = GameObject.FindObjectsOfType<MoveObject>();
			MoveObject closestLight = cls[0];
			foreach(MoveObject cl in cls)
            {
				if(Vector3.Distance(cl.transform.position, pos) < Vector3.Distance(closestLight.transform.position, pos))
                {
					closestLight = cl;
				}
			}
			InspectorCanvasScript.ServerDeleteLight(closestLight.gameObject,GameObject.Find("tileHighlight"));
		}
		else if (id == "doc") //done
		{
			//InfoLogCanvasScript.SendInfoMessage("Door Toggled" + " - Sent From Server", UnityEngine.Color.black, 9, false);
			Vector2Int pos = new Vector2Int(int.Parse(info.Split(new char[1] { '|' })[0]), int.Parse(info.Split(new char[1] { '|' })[1]));

			InspectorCanvasScript.ServerDoorClicked(Manager.Instance.tiles[pos.x, pos.y].gameObject);
		}
		else if (id == "ocr") //done
		{
			//InfoLogCanvasScript.SendInfoMessage("Object Created" + " - Sent From Server", UnityEngine.Color.black, 9, false);
			Manager.Instance.mainCanvas.ServerMakeObject(new Vector3(float.Parse(info.Split(new char[1] { '|' })[0]), float.Parse(info.Split(new char[1] { '|' })[1]), 0));
		}
		else if (id == "rtm") //done
		{
			InfoLogCanvasScript.SendInfoMessage("Returning to Main Menu", UnityEngine.Color.black, 9, false);
			try
			{
				SettingsCanvasScript.ServerReturnToMainMenu();
			}
			catch
            {
				InfoLogCanvasScript.SendInfoMessage("Already at Main Menu", UnityEngine.Color.black, 9, false);
			}
		}
		else if (id == "glt") //done
		{
			//InfoLogCanvasScript.SendInfoMessage("Toggled global lighting" + " - Sent From Server", UnityEngine.Color.black, 9, false);
			Manager.Instance.mainCanvas.ServerToggleLighting();
		}
		else if (id == "fvc") //done
		{
			bool closeAfterFinish = true;
			if(!Manager.Instance.lightMenu.activeSelf)
            {
				Manager.Instance.OpenLightMenu();
			}
			else
            {
				closeAfterFinish = false;
			}
			string[] infoArray = info.Split(new char[1] { '|' });
			List<Vector2> pointsList = new List<Vector2>();
			if(infoArray[2] != "X")
            {
				for(int i = 2; i < infoArray.Length - 1; i += 2)
                {
					pointsList.Add(new Vector2(float.Parse(infoArray[i]), float.Parse(infoArray[i + 1])));
                }
            }
			Manager.Instance.lightMenu.GetComponent<LightTool>().ServerHandleLightChange(int.Parse(infoArray[0]), infoArray[1] == "1" ? true : false, pointsList, infoArray[infoArray.Length - 1] == "1" ? true : false, closeAfterFinish);
		}
		else if (id == "fcr") //done
		{
			bool closeAfterFinish = true;
			if (!Manager.Instance.lightMenu.activeSelf)
			{
				Manager.Instance.OpenLightMenu();
			}
			else
			{
				closeAfterFinish = false;
			}
			string[] infoArray = info.Split(new char[1] { '|' });
			List<Vector2> pointsList = new List<Vector2>();
			for (int i = 0; i < infoArray.Length - 1; i += 2)
			{
				pointsList.Add(new Vector2(float.Parse(infoArray[i]), float.Parse(infoArray[i + 1])));
			}
			//InfoLogCanvasScript.SendInfoMessage("Freeform light area created" + " - Sent From Server", UnityEngine.Color.black, 9, false);
			Manager.Instance.lightMenu.GetComponent<LightTool>().ServerCreateShape(pointsList, closeAfterFinish);
		}
		else if (id == "svc") //done
		{
			bool closeAfterFinish = true;
			if (!Manager.Instance.shadowMenu.activeSelf)
			{
				Manager.Instance.OpenShadowMenu();
			}
			else
			{
				closeAfterFinish = false;
			}
			string[] infoArray = info.Split(new char[1] { '|' });
			List<Vector2> pointsList = new List<Vector2>();
			if (infoArray[2] != "X")
			{
				for (int i = 2; i < infoArray.Length - 1; i += 2)
				{
					pointsList.Add(new Vector2(float.Parse(infoArray[i]), float.Parse(infoArray[i + 1])));
				}
			}
			Manager.Instance.shadowMenu.GetComponent<ShadowTool>().ServerHandleShadowChange(int.Parse(infoArray[0]), infoArray[1] == "1" ? true : false, pointsList, infoArray[infoArray.Length - 1] == "1" ? true : false, closeAfterFinish);
		}
		else if (id == "scr") //done
		{
			bool closeAfterFinish = true;
			if (!Manager.Instance.shadowMenu.activeSelf)
			{
				Manager.Instance.OpenShadowMenu();
			}
			else
			{
				closeAfterFinish = false;
			}
			string[] infoArray = info.Split(new char[1] { '|' });
			List<Vector2> pointsList = new List<Vector2>();
			for (int i = 0; i < infoArray.Length - 1; i += 2)
			{
				pointsList.Add(new Vector2(float.Parse(infoArray[i]), float.Parse(infoArray[i + 1])));
			}
			//InfoLogCanvasScript.SendInfoMessage("Shadow area created" + " - Sent From Server", UnityEngine.Color.black, 9, false);
			Manager.Instance.shadowMenu.GetComponent<ShadowTool>().ServerCreateShape(pointsList, closeAfterFinish);
		}
		else if (id == "ovc") //done
		{
			//InfoLogCanvasScript.SendInfoMessage("Object Values Changed" + " - Sent From Server", UnityEngine.Color.black, 9, false);
			string[] splitInfo = info.Split(new char[1] { '|' });
			Vector2 pos = new Vector2(float.Parse(splitInfo[0]), float.Parse(splitInfo[1]));

			MoveObject[] cls = GameObject.FindObjectsOfType<MoveObject>();
			MoveObject closestLight = cls[0];
			foreach (MoveObject cl in cls)
			{
				if (Vector3.Distance(cl.transform.position, pos) < Vector3.Distance(closestLight.transform.position, pos))
				{
					closestLight = cl;
				}
			}

			GameObject closestObject = closestLight.gameObject;

			if(splitInfo[2] != "X")
            {
				float scaleMultiplier = float.Parse(splitInfo[2]) / closestObject.GetComponentInChildren<CustomLight>().boundingSphereRadius;
				closestObject.GetComponentInChildren<CustomLight>().Scale(closestObject.transform.position, scaleMultiplier);
				closestObject.GetComponentInChildren<Light2D>().pointLightOuterRadius = closestObject.GetComponentInChildren<CustomLight>().boundingSphereRadius * 1.65f;
				Manager.Instance.FindElementsToManage();
				Manager.Instance.AssignShapes();
				Manager.Instance.BuildAllLights();
			}
			else if (splitInfo[3] != "X")
			{
				closestObject.GetComponentInChildren<SpriteRenderer>().transform.localRotation = Quaternion.Euler(closestObject.GetComponentInChildren<SpriteRenderer>().transform.localRotation.eulerAngles.x, closestObject.GetComponentInChildren<SpriteRenderer>().transform.localRotation.eulerAngles.y, int.Parse(splitInfo[3]));
			}
			else if (splitInfo[4] != "X")
			{
				closestObject.GetComponentInChildren<SpriteRenderer>().transform.localScale = new Vector3(float.Parse(splitInfo[4]), float.Parse(splitInfo[4]), closestObject.GetComponentInChildren<SpriteRenderer>().transform.localScale.z);
			}
			else if (splitInfo[5] != "X")
			{
				closestObject.GetComponentInChildren<CustomLight>().functional = !closestObject.GetComponentInChildren<CustomLight>().functional;
				if (closestObject.GetComponentInChildren<Light2D>().pointLightInnerRadius > 0.001f)
				{
					closestObject.GetComponentInChildren<Light2D>().pointLightInnerRadius = 0;
				}
				else
				{
					closestObject.GetComponentInChildren<Light2D>().pointLightInnerRadius = float.MaxValue;
				}
				Manager.Instance.FindElementsToManage();
				Manager.Instance.AssignShapes();
				Manager.Instance.BuildAllLights();
			}
		}
		else if (id == "opc") //done
		{
			//InfoLogCanvasScript.SendInfoMessage("Object Position Changed" + " - Sent From Server", UnityEngine.Color.black, 9, false);
			string[] splitInfo = info.Split(new char[1] { '|' });
			Vector2 pos = new Vector2(float.Parse(splitInfo[0]), float.Parse(splitInfo[1]));

			MoveObject[] cls = GameObject.FindObjectsOfType<MoveObject>();
			MoveObject closestLight = cls[0];
			foreach (MoveObject cl in cls)
			{
				if (Vector3.Distance(cl.transform.position, pos) < Vector3.Distance(closestLight.transform.position, pos))
				{
					closestLight = cl;
				}
			}

			GameObject closestObject = closestLight.gameObject;

			if (splitInfo[3] == "X")
			{
				Vector2 mousePos = new Vector2(float.Parse(splitInfo[5]), float.Parse(splitInfo[6]));
				closestObject.transform.position = mousePos;
				if (splitInfo[2] == "1")
				{
					closestObject.GetComponentInChildren<CustomLight>().Translate(mousePos - (Vector2)closestObject.transform.position);
				}
			}
			else
			{
				Tile t = Manager.Instance.tiles[int.Parse(splitInfo[3]), int.Parse(splitInfo[4])];
				closestObject.GetComponent<MoveObject>().gridSnappedTile = t;
				closestObject.transform.position = t.GetComponent<SpriteRenderer>().bounds.center;
				if (splitInfo[2] == "1")
				{
					closestObject.GetComponentInChildren<CustomLight>().Translate(t.GetComponent<SpriteRenderer>().bounds.center - (Vector3)closestObject.GetComponentInChildren<CustomLight>().center);
				}
			}
			closestObject.GetComponent<MoveObject>().networkReadyForUpdate = true;
		}
		else if (id == "tsc") //done
		{
			InfoLogCanvasScript.SendInfoMessage("Loading Tile Set", UnityEngine.Color.green);

			string[] splitInfo = info.Split(new char[1] { '|' });
			//Check that each file path exists, if it does not, create an image in that folder
			int fromID = int.Parse(splitInfo[splitInfo.Length - 1]);
			List<string> filePaths = new List<string>();
			for(int i = 0; i < splitInfo.Length - 1; i++)
            {
				filePaths.Add(splitInfo[i]);
			}

			missingFileCount = 0;

			for (int i = 0; i < filePaths.Count; i++)
            {
				if(!File.Exists(filePaths[i]))
                {
					missingFileCount++;
					//Setup the file path
					string[] splitFilePath = filePaths[i].Split(new char[1] { '\\' });
					for (int x = 0; x < splitFilePath.Length; x++)
					{
						if (!splitFilePath[x].Contains(".png") && !splitFilePath[x].Contains(".jpg") && !splitFilePath[x].Contains(".jpeg"))
						{
							string currentDirectory = "";
							for (int c = 0; c <= x; c++)
							{
								currentDirectory += splitFilePath[c] + "\\";
							}

							if (!Directory.Exists(currentDirectory))
							{
								Directory.CreateDirectory(currentDirectory);
							}
						}
						else
						{
							break;
						}
					}

					//Request the missing image from the sender
					Connection.SendMessage(Encoding.UTF8.GetBytes($"tor{fromID}|{filePaths[i]}"));
				}
			}
			//Load the set from the root file path
			if (missingFileCount == 0)
			{
				string setDirectory = "";
				string[] splitPath = filePaths[0].Split(new char[1] { '\\' });
				for (int c = 0; c < splitPath.Length; c++)
				{
					if (!splitPath[c].Contains(".png") && !splitPath[c].Contains(".jpg") && !splitPath[c].Contains(".jpeg"))
					{
						setDirectory += splitPath[c] + "\\";
					}
				}

				Manager.Instance.ServerLoadTileSet(setDirectory); 
			}
		}
		else if (id == "itl") //done
		{
			string[] splitInfo = info.Split(new char[1] { '|' });

			byte[] byteLengthArray = Encoding.UTF8.GetBytes("itl" + splitInfo[0] + "|" + splitInfo[1] + "|" + splitInfo[2] + "|");
			int start = byteLengthArray.Length;
			byte[] imageDataT = new byte[size];
			Marshal.Copy(data, imageDataT, 0, size);

			List<byte> imageDataL = imageDataT.ToList();

			//byte[] imageData = new byte[size - start];
			imageDataL.RemoveRange(0, start);
			byte[] imageData = imageDataL.ToArray();

			//Save the image data
			File.WriteAllBytes(splitInfo[2], imageData);

			missingFileCount--;
			if(missingFileCount < 1)
            {
				string setDirectory = "";
				string[] splitPath = splitInfo[2].Split(new char[1] { '\\' });
				for (int c = 0; c < splitPath.Length; c++)
				{
					if (!splitPath[c].Contains(".png") && !splitPath[c].Contains(".jpg") && !splitPath[c].Contains(".jpeg"))
					{
						setDirectory += splitPath[c] + "\\";
					}
				}

				Manager.Instance.ServerLoadTileSet(setDirectory);
			}
		}
		else if (id == "did") //done
		{
			//InfoLogCanvasScript.SendInfoMessage("Downloading Image Data " + " - Sent From Server", UnityEngine.Color.green);

			string[] splitInfo = info.Split(new char[1] { '|' });
			//Check that each file path exists, if it does not, create an image in that folder
			List<string> filePaths = new List<string>();
			for (int i = 0; i < splitInfo.Length; i++)
			{
				filePaths.Add(splitInfo[i]);
			}
			int lol = textureDataCache.Count;
			for (int i = 0; i < filePaths.Count; i++)
			{
				if (!File.Exists(filePaths[i]))
				{
					//InfoLogCanvasScript.SendInfoMessage(i.ToString(), UnityEngine.Color.green);
					//Setup the file path
					string[] splitFilePath = filePaths[i].Split(new char[1] { '\\' });
					for (int x = 0; x < splitFilePath.Length; x++)
					{
						if (!splitFilePath[x].Contains(".png") && !splitFilePath[x].Contains(".jpg") && !splitFilePath[x].Contains(".jpeg"))
						{
							string currentDirectory = "";
							for (int c = 0; c <= x; c++)
							{
								currentDirectory += splitFilePath[c] + "\\";
							}

							if (!Directory.Exists(currentDirectory))
							{
								Directory.CreateDirectory(currentDirectory);
							}
						}
						else
						{
							break;
						}
					}

					//Save the image data
					File.WriteAllBytes(filePaths[i], textureDataCache[i]);
				}
			}
			InfoLogCanvasScript.SendInfoMessage("C", UnityEngine.Color.green);
			textureDataCache = new List<byte[]>();
			InfoLogCanvasScript.SendInfoMessage("Completed writing image data", UnityEngine.Color.black, 9, false);
		}
		else if (id == "lmp") //done
		{
			InfoLogCanvasScript.SendInfoMessage("Loading Map ", UnityEngine.Color.green);

			if(File.Exists(@"GameLoadedAssets\Maps\SavedMaps\ServerMapSaveTemp.txt"))
            {
				File.Delete(@"GameLoadedAssets\Maps\SavedMaps\ServerMapSaveTemp.txt");
            }
			File.WriteAllBytes(@"GameLoadedAssets\Maps\SavedMaps\ServerMapSaveTemp.txt", textureDataCache[0]);

			DungeonFile.fileType = FileType.SavedMap;
			DungeonFile.fromSaveFile = true;
			DungeonFile.path = @"GameLoadedAssets\Maps\SavedMaps\ServerMapSaveTemp.txt";

			GameObject.FindObjectOfType<FileSelect>().LoadDungeon();

			textureDataCache = new List<byte[]>();
			InfoLogCanvasScript.SendInfoMessage("Completed map load", UnityEngine.Color.black, 9, false);
		}
		else if (id == "dmd") //done
		{
			InfoLogCanvasScript.SendInfoMessage("Adding Map Data", UnityEngine.Color.green);

			if (File.Exists(info))
			{
				File.Delete(info);
			}
			File.WriteAllBytes(info, textureDataCache[0]);

			textureDataCache = new List<byte[]>();
		}
		else if (id == "rms") //done
		{
			InfoLogCanvasScript.SendInfoMessage("Returning to Main Menu", UnityEngine.Color.black, 9, false);
			try
			{
				SettingsCanvasScript.ServerReturnToMainMenuNoDetatch();
			}
			catch
			{
				InfoLogCanvasScript.SendInfoMessage("Already at Main Menu", UnityEngine.Color.black, 9, false);
			}
		}
		else if (id == "cpt") //done
		{
			Manager.Instance.inspector.CloseIconSelect();
			string[] splitInfo = info.Split(new char[1] { '|' });

			//Setup the file path
			string[] splitFilePath = editPath.Split(new char[1] { '\\' });
			for (int i = 0; i < splitFilePath.Length; i++)
			{
				if (!splitFilePath[i].Contains(".png") && !splitFilePath[i].Contains(".jpg") && !splitFilePath[i].Contains(".jpeg"))
				{
					string currentDirectory = "";
					for (int c = 0; c <= i; c++)
					{
						currentDirectory += splitFilePath[c] + "\\";
					}

					if (!Directory.Exists(currentDirectory))
					{
						Directory.CreateDirectory(currentDirectory);
					}
				}
				else
				{
					break;
				}
			}

			byte[] byteLengthArray = Encoding.UTF8.GetBytes("cpt" + splitInfo[0] + "|" + splitInfo[1] + "|");
			int start = byteLengthArray.Length;
			byte[] imageDataT = new byte[size];
			Marshal.Copy(data, imageDataT, 0, size);

			List<byte> imageDataL = imageDataT.ToList();

			//byte[] imageData = new byte[size - start];
			imageDataL.RemoveRange(0, start);
			byte[] imageData = imageDataL.ToArray();

			//Save the image data
			File.WriteAllBytes(editPath, imageData);
			Texture2D texture = new Texture2D(int.Parse(splitInfo[0]), int.Parse(splitInfo[1]));
			texture.LoadImage(imageData);
			textureDataCache = new List<byte[]>();
			Sprite sprite;
			if (texture.width < texture.height)
			{
				sprite = Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.width, texture.width)), new Vector2(0.5f, 0.5f), texture.width);
			}
			else if (texture.height < texture.width)
			{
				sprite = Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.height, texture.height)), new Vector2(0.5f, 0.5f), texture.height);
			}
			else
			{
				sprite = Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.width, texture.height)), new Vector2(0.5f, 0.5f), texture.width);
			}
			Manager.Instance.tiles[editTile.x, editTile.y].GetComponentInChildren<SpriteRenderer>().sprite = sprite;
			Manager.Instance.tiles[editTile.x, editTile.y].tileSpritePath = splitInfo[0];
		}
		else if (id == "sct") //done
		{
			string[] splitInfo = info.Split(new char[1] { '|' });
			bool filePathExists = File.Exists(splitInfo[0]);
			Vector2Int pos = new Vector2Int(int.Parse(splitInfo[1]), int.Parse(splitInfo[2]));
			int fromID = int.Parse(splitInfo[3]);

			if (filePathExists)
			{
				Manager.Instance.inspector.CloseIconSelect();
				textureDataCache = new List<byte[]>();

				//InfoLogCanvasScript.SendInfoMessage("Tile Sprite Changed From Client Files" + " - Sent From Server", UnityEngine.Color.black, 9, false);

				FileStream stream = new FileStream(splitInfo[0], FileMode.Open);
				Bitmap image = new Bitmap(stream);
				int textureWidth = image.Width;
				int textureHeight = image.Height;
				stream.Close();
				Texture2D texture = new Texture2D(textureWidth, textureHeight);
				texture.LoadImage(File.ReadAllBytes(splitInfo[0]));
				Sprite sprite;
				if (texture.width < texture.height)
				{
					sprite = Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.width, texture.width)), new Vector2(0.5f, 0.5f), texture.width);
				}
				else if (texture.height < texture.width)
				{
					sprite = Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.height, texture.height)), new Vector2(0.5f, 0.5f), texture.height);
				}
				else
				{
					sprite = Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.width, texture.height)), new Vector2(0.5f, 0.5f), texture.width);
				}
				Manager.Instance.tiles[pos.x, pos.y].GetComponentInChildren<SpriteRenderer>().sprite = sprite;
				Manager.Instance.tiles[pos.x, pos.y].tileSpritePath = splitInfo[0];
			}
			else
			{
				//InfoLogCanvasScript.SendInfoMessage("Tile Sprite Changed From Server" + " - Sent From Server", UnityEngine.Color.black, 9, false);
				editPath = splitInfo[0];
				editTile = pos;
				Connection.SendMessage(Encoding.UTF8.GetBytes($"idr{fromID}"));
			}
		}
		else if (id == "cpo") //done
		{
			Manager.Instance.inspector.CloseIconSelect();
			string[] splitInfo = info.Split(new char[1] { '|' });

			//Setup the file path
			string[] splitFilePath = editPath.Split(new char[1] { '\\' });
			for (int i = 0; i < splitFilePath.Length; i++)
			{
				if (!splitFilePath[i].Contains(".png") && !splitFilePath[i].Contains(".jpg") && !splitFilePath[i].Contains(".jpeg"))
				{
					string currentDirectory = "";
					for (int c = 0; c <= i; c++)
					{
						currentDirectory += splitFilePath[c] + "\\";
					}

					if (!Directory.Exists(currentDirectory))
					{
						Directory.CreateDirectory(currentDirectory);
					}
				}
				else
				{
					break;
				}
			}

			byte[] byteLengthArray = Encoding.UTF8.GetBytes("cpo" + splitInfo[0] + "|" + splitInfo[1] + "|");
			int start = byteLengthArray.Length;
			byte[] imageDataT = new byte[size];
			Marshal.Copy(data, imageDataT, 0, size);

			List<byte> imageDataL = imageDataT.ToList();

			//byte[] imageData = new byte[size - start];
			imageDataL.RemoveRange(0, start);
			byte[] imageData = imageDataL.ToArray();

			//Save the image data
			File.WriteAllBytes(editPath, imageData);
			Texture2D texture = new Texture2D(int.Parse(splitInfo[0]), int.Parse(splitInfo[1]));
			texture.LoadImage(imageData);
			textureDataCache = new List<byte[]>();
			Sprite sprite;
			if (texture.width < texture.height)
			{
				sprite = Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.width, texture.height)), new Vector2(0.5f, 0.5f), texture.height);
			}
			else if (texture.height < texture.width)
			{
				sprite = Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.height, texture.width)), new Vector2(0.5f, 0.5f), texture.width);
			}
			else
			{
				sprite = Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.width, texture.height)), new Vector2(0.5f, 0.5f), texture.width);
			}

			MoveObject[] cls = GameObject.FindObjectsOfType<MoveObject>();
			MoveObject closestLight = cls[0];
			foreach (MoveObject cl in cls)
			{
				if (Vector3.Distance(cl.transform.position, editObject) < Vector3.Distance(closestLight.transform.position, editObject))
				{
					closestLight = cl;
				}
			}

			closestLight.gameObject.GetComponentInChildren<SpriteRenderer>().sprite = sprite;
			closestLight.spritePath = splitInfo[0];
		}
		else if (id == "sco") //done
		{
			string[] splitInfo = info.Split(new char[1] { '|' });
			bool filePathExists = File.Exists(splitInfo[0]);
			Vector2 pos = new Vector2(float.Parse(splitInfo[1]), float.Parse(splitInfo[2]));
			int fromID = int.Parse(splitInfo[3]);

			if (filePathExists)
			{
				Manager.Instance.inspector.CloseIconSelect();
				textureDataCache = new List<byte[]>();

				//InfoLogCanvasScript.SendInfoMessage("Object Sprite Changed From Client Files" + " - Sent From Server", UnityEngine.Color.black, 9, false);

				FileStream stream = new FileStream(splitInfo[0], FileMode.Open);
				Bitmap image = new Bitmap(stream);
				int textureWidth = image.Width;
				int textureHeight = image.Height;
				stream.Close();
				Texture2D texture = new Texture2D(textureWidth, textureHeight);
				texture.LoadImage(File.ReadAllBytes(splitInfo[0]));
				Sprite sprite;
				if (texture.width < texture.height)
				{
					sprite = Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.width, texture.height)), new Vector2(0.5f, 0.5f), texture.height);
				}
				else if (texture.height < texture.width)
				{
					sprite = Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.height, texture.width)), new Vector2(0.5f, 0.5f), texture.width);
				}
				else
				{
					sprite = Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.width, texture.height)), new Vector2(0.5f, 0.5f), texture.width);
				}

				MoveObject[] cls = GameObject.FindObjectsOfType<MoveObject>();
				MoveObject closestLight = cls[0];
				foreach (MoveObject cl in cls)
				{
					if (Vector3.Distance(cl.transform.position, pos) < Vector3.Distance(closestLight.transform.position, pos))
					{
						closestLight = cl;
					}
				}

				closestLight.gameObject.GetComponentInChildren<SpriteRenderer>().sprite = sprite;
				closestLight.spritePath = splitInfo[0];
			}
			else
			{
				//InfoLogCanvasScript.SendInfoMessage("Object Sprite Changed From Server" + " - Sent From Server", UnityEngine.Color.black, 9, false);
				editPath = splitInfo[0];
				editObject = pos;
				Connection.SendMessage(Encoding.UTF8.GetBytes($"ior{fromID}"));
			}
		}
		else if (id == "fdr") //done
		{
			string[] splitInfo = info.Split(new char[1] { '|' });
			Texture2D lt = GameObject.FindObjectOfType<IconSelectCanvasScript>().lastUsedSpriteTexture;
			byte[] PNGBytes = lt.EncodeToPNG();
			Connection.SendMessage(Encoding.UTF8.GetBytes($"ids{splitInfo[0]}|{lt.width}|{lt.height}|").Concat(PNGBytes).ToArray());
		}
		else if (id == "for") //done
		{
			string[] splitInfo = info.Split(new char[1] { '|' });
			Texture2D lt = GameObject.FindObjectOfType<IconSelectCanvasScript>().lastUsedSpriteTexture;
			byte[] PNGBytes = lt.EncodeToPNG();
			Connection.SendMessage(Encoding.UTF8.GetBytes($"ios{splitInfo[0]}|{lt.width}|{lt.height}|").Concat(PNGBytes).ToArray());
		}
		else if (id == "fts") //done
		{
			string[] splitInfo = info.Split(new char[1] { '|' });
			string filePath = splitInfo[1];
			if(splitInfo.Length > 2)
            {
				for(int i = 2; i < splitInfo.Length; i++)
                {
					filePath += "|" + splitInfo[i];
				}
            }

			FileStream stream = new FileStream(filePath, FileMode.Open);
			Bitmap image = new Bitmap(stream);
			int textureWidth = image.Width;
			int textureHeight = image.Height;
			stream.Close();
			Texture2D lt = new Texture2D(textureWidth, textureHeight);
			lt.LoadImage(File.ReadAllBytes(filePath));
			byte[] PNGBytes = lt.EncodeToPNG();

			Connection.SendMessage(Encoding.UTF8.GetBytes($"tos{splitInfo[0]}|{lt.width}|{lt.height}|{filePath}|").Concat(PNGBytes).ToArray());
		}
		else
        {
			//This is image data
			if(textureDataCache == null)
			{
				textureDataCache = new List<byte[]>();
			}
			int permSize = size;
			textureDataCache.Add(new byte[permSize]);
			Marshal.Copy(data, textureDataCache[textureDataCache.Count - 1], 0, size);
		}
	}

	internal async Task RunAsync()
	{
		while (preDisconnect)
		{
			while (serverConnected)
			{
				await Task.Delay(10);
				Receive();
			}
			await Task.Delay(50);
		}
	}
}
