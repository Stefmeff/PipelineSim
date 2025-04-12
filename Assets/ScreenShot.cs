using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class ScreenShot : MonoBehaviour
{
    private int i = 0;


    // Update is called once per frame
    void Update()
    {
        TakeScreenShot();
    }

    private void TakeScreenShot(){

        if(Input.GetKeyDown(KeyCode.Space)){
            string filename = "Screenshot" + i + ".png";
            ScreenCapture.CaptureScreenshot(filename,2);
            i++;
        }
    }
}
