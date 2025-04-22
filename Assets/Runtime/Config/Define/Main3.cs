using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RunTime.Config.Define
{

    public static class Main3Static  
	{
		public static void Test()
		{
			Debug.Log("Main3Static.test");
		}
	}


    public class Main3 : MonoBehaviour
	{
		// Start is called before the first frame update
		void Start()
		{
			Lancher.Game.Stage.Slot.Main2 xx = gameObject.GetComponent<Lancher.Game.Stage.Slot.Main2>();

        }

		// Update is called once per frame
		void Update()
		{
			
		}
	}
}

