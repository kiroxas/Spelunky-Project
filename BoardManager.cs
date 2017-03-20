using UnityEngine;
using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using UnityStandardAssets.Utility;

namespace FileUtils
{
	[XmlRoot(ElementName="FileList")]
	public class FileList
	{
		[XmlArray("files")]
		[XmlArrayItem("file")]
		public List<string> files{ get; set;}
		
		public List<List<int>> loaded{ get; set;}
		
		public List<int> getOneRandom()
		{
			int index = Random.Range(0, loaded.Count);
			return loaded[index];
		}
	}
}



public class BoardManager : MonoBehaviour {
	
	public int width = 4;
	public int height = 4;
	
	public float tileWidth = 1.28f;
	public float tileHeight = 1.28f;
	
	public GameObject[] undestructibleTile;
	public GameObject[] landTiles;
	public GameObject player;
	public GameObject[] doors;
	public GameObject camera;
	
	GameObject instancePlayer;
	
	public float topLeftXPos = 0;
	public float topLeftYPos = 0;
	
	int entryIndex = 0;
	int exitIndex = 0;
	
	int roomX = 10;
	int roomY = 8;
	
	List<int> map = new List<int>();
	
	void undestructibleAroundRoom()
	{
		float yStart = topLeftYPos + tileHeight;
		for(int x = 0; x < roomX * width + 2; ++x)
		{
			GameObject instance = undestructibleTile[Random.Range(0, landTiles.Length)];
			UnityEngine.Object.Instantiate(instance, new Vector3(topLeftXPos - tileWidth + x * tileWidth, yStart, 0),  Quaternion.identity);
		}
		
		float yEnd = topLeftYPos - tileHeight * roomY * height;
		for(int x = 0; x < roomX * width + 2; ++x)
		{
			GameObject instance = undestructibleTile[Random.Range(0, landTiles.Length)];
			UnityEngine.Object.Instantiate(instance, new Vector3(topLeftXPos - tileWidth + x * tileWidth, yEnd, 0),  Quaternion.identity);
		}
		
		float xStart = topLeftXPos - tileHeight;
		for(int y = 0; y < roomY * height + 1; ++y)
		{
			GameObject instance = undestructibleTile[Random.Range(0, landTiles.Length)];
			UnityEngine.Object.Instantiate(instance, new Vector3(xStart, topLeftYPos - y * tileHeight, 0),  Quaternion.identity);
		}
		
		float xEnd = topLeftXPos + tileHeight * roomX * width;
		for(int y = 0; y < roomY * height + 1; ++y)
		{
			GameObject instance = undestructibleTile[Random.Range(0, landTiles.Length)];
			UnityEngine.Object.Instantiate(instance, new Vector3(xEnd, topLeftYPos - y * tileHeight, 0),  Quaternion.identity);
		}
	}
	
	void goDown(ref List<int> map, ref int index, ref bool finished)
	{
		if(index / width == height - 1)
		{
			exitIndex = index;
			finished = true;
		}
		else
		{	
			if(map[index] == 2 || map[index] == 3)
			{			
				map[index] = 4;
			}
			else
			{
				map[index] = 2;
			}
			index += width;	
			map[index] = 3;
		}
	}
	
	FileUtils.FileList loadFileList(string folder)
	{
		XmlSerializer serial = new XmlSerializer(typeof(FileUtils.FileList));
        Stream reader = new FileStream(folder + "file_list.xml", FileMode.Open);
        FileUtils.FileList list = (FileUtils.FileList)serial.Deserialize(reader);
		
		foreach(string f in list.files)
		{
			list.loaded.Add(toInt(loadTileGroup(folder, f)));	
		}
		
		return list;
	}
	
	List<int> toInt(string[] lines)
	{
		List<int> list = new List<int>();
		
		foreach(string s in lines)
		{
			foreach(char c in s)
			{
				list.Add((int)Char.GetNumericValue(c));
			}
		}
		
		return list;
	}
	
	string[] loadTileGroup(string folder, string name)
	{
		 string[] lines = System.IO.File.ReadAllLines(@folder + name);

		 return lines;
	}

	void createRoom(List<int> layout, float topLeftX, float topLeftY, bool putDoor, bool putPlayer)
	{
		for(int y = 0; y < roomY; ++y)
		{
			for(int x = 0; x < roomX; ++x)
			{
				if(layout[y * roomX + x] == 1)
				{
					GameObject instance = landTiles[Random.Range(0, landTiles.Length)];
					UnityEngine.Object.Instantiate(instance, new Vector3(topLeftX + x * tileWidth, topLeftY - y * tileHeight, 0),  Quaternion.identity);
				}
			}
		}
	
		while(putDoor)
		{
			int index = Random.Range(0, layout.Count);
			int actualIndex = index - roomX;
			if(layout[index] == 1 && actualIndex >= 0 && actualIndex < layout.Count && layout[actualIndex] == 0)
			{
				int x = actualIndex % roomX;
				int y = (actualIndex / roomY) - 1;
				Debug.LogError("Door in : " + x + " " + y + " " + actualIndex);
				GameObject instance = doors[Random.Range(0, doors.Length)];
				UnityEngine.Object.Instantiate(instance, new Vector3(topLeftX + x * tileWidth, topLeftY - y * tileHeight, 0),  Quaternion.identity);
				putDoor = false;
				if(putPlayer)
				{
					instancePlayer = (GameObject)UnityEngine.Object.Instantiate(player, new Vector3(topLeftX + x * tileWidth, topLeftY - y * tileHeight, 0),  Quaternion.identity);
				}
			}
		}
	}

	void makeCameraFollowPlayer()
	{
		Transform trans = (Transform)instancePlayer.GetComponent("Transform");
		
		SmoothFollow f = (SmoothFollow)(camera.GetComponent("SmoothFollow"));
		f.followHim(trans);
	}
	
	void fillMap()
	{
		// Let's decide the shape of the maze (http://tinysubversions.com/spelunkyGen/)
		// 0 rooms are any kind of rooms
		// 1 room with left/right exits
		// 2 room with l/r and south
		// 3, room with l/r and top
		// 4, with all exits (an old 2 with 2 above)
		// 5 room, with entrance or exit
		
		entryIndex = Random.Range(0, width);
		int index = entryIndex;
		
		map = new List<int>(new int[width * height]);
		
		map[entryIndex] = 1; // For now, entrance is always a one
		bool finished = false;
		
		while(!finished)
		{
			int direction = Random.Range(0, 5);
			if(direction < 2) // Go LEft
			{
				if(index % width == 0)
				{
					goDown(ref map, ref index, ref finished);				
				}
				else
				{
					index--;
					if(map[index] == 0)
					{
						map[index] = 1;
					}
				}
			}
			else if( direction < 4) // Go right
			{
				if(index % width == width - 1)
				{
					goDown(ref map, ref index, ref finished);
				}
				else 
				{
					index++;
					if(map[index] == 0)
					{
						map[index] = 1;
					}
				}
			}
			else // Down !
			{
				goDown(ref map, ref index, ref finished);	 
			}
		}
	}
	
	// Use this for initialization
	void Start () 
	{
		// the level layout
		fillMap();
		
		// Now load the room prefabs
		List<FileUtils.FileList> typeRooms = new List<FileUtils.FileList>();
		
		typeRooms.Add(loadFileList("Assets/Standard Assets/room_prefab/type_0/"));
		typeRooms.Add(loadFileList("Assets/Standard Assets/room_prefab/type_1/"));
		typeRooms.Add(loadFileList("Assets/Standard Assets/room_prefab/type_2/"));
		typeRooms.Add(loadFileList("Assets/Standard Assets/room_prefab/type_3/"));
		typeRooms.Add(loadFileList("Assets/Standard Assets/room_prefab/type_4/"));
		
		for(int x = 0; x < width; ++x)
		{
			for(int y = 0; y < height; ++y)
			{
				int index = x + y * width;
				int room = map[index];
				List<int> layout;
				if(room != 0)
				{
					layout = typeRooms[room].getOneRandom();
				}
				else
				{
					layout = typeRooms[Random.Range(0, typeRooms.Count)].getOneRandom();
				}
				
				bool putDoor = index == entryIndex || index == exitIndex;
				bool putPlayer = index == entryIndex;
			
				float topX = topLeftXPos + x * roomX * tileWidth;
				float topY = topLeftYPos - y * roomY * tileHeight;
			
				createRoom(layout, topX, topY, putDoor, putPlayer);
			}
		}
		
		undestructibleAroundRoom();
		makeCameraFollowPlayer();
	}
}