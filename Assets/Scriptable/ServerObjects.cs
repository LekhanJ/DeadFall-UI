using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "Server_Objects", menuName = "Scriptable Objects/Server Objects", order = 3)]
public class ServerObjects : ScriptableObject
{
    public List<ServerObjectData> Objects;

    public ServerObjectData GetObjectByName(string name)
    {
        return Objects.SingleOrDefault(x => x.Name == name);
    }
}

[Serializable]
public class ServerObjectData
{
    public string Name = "New Object";
    public GameObject Prefab;
}
