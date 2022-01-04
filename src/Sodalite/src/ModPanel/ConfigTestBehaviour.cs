using BepInEx.Configuration;
using UnityEngine;

namespace Sodalite.ModPanel;

public class ConfigTestBehaviour : MonoBehaviour
{
	private void Awake()
	{
		ConfigFile config = new ConfigFile("myConfig.cfg", false);

	}
}
