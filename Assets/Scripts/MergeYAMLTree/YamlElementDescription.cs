using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "YamlElementDescription", menuName = "ScriptableObjects/YamlElementDescription")]
public class YamlElementDescription : ScriptableObject
{
    [Serializable]
    public class DescriptionData
    {
        public string Name;
        [TextArea] public string Description; 
    }

    public List<DescriptionData> Descriptions = new List<DescriptionData>();
}
