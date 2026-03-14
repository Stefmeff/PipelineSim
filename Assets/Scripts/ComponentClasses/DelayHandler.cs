using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public static class DelayHandler
{
    private static Texture2D texture = new Texture2D(1, 1);
    private static Sprite squareSprite;
    static DelayHandler()
    {
        // Create a white square texture
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        squareSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0, 0.5f));
    }


    public static BitToken CheckDelayQueue(List<Tuple<BitToken, GameObject>> signalQueue, int delay, int tick){
        if (signalQueue.Count > 1)
        {

            BitToken nextOut = signalQueue[1].Item1;
            int arrivalTime = nextOut.GetTime();

            //check if next output signal is ready
            if (arrivalTime + delay <= tick)
            {
                //set output and remove from signal queue
                if(signalQueue[0].Item2 != null) GameObject.Destroy(signalQueue[0].Item2);
                signalQueue.RemoveAt(0);
                return nextOut.NewToken(arrivalTime + delay);  
            }
        } 
        return signalQueue[0].Item1;
    }

    public static void DrawSquares(List<Tuple<BitToken, GameObject>> signalQueue, int delay, int tick){
        foreach(Tuple<BitToken,GameObject> t in signalQueue){
            if(delay == 0){
                t.Item2.transform.localScale = new Vector3(100, 100, 1);
            }else{
                int passedTime = tick-t.Item1.GetTime();

                if(passedTime <= delay && passedTime >= 0){
                    //calculate length of square
                    float length = 100*passedTime/delay;
                    t.Item2.transform.localScale = new Vector3(length, 100, 1);
                }
            }
   
        }  
    }

    public static GameObject NewSquare(float length, Color c, GameObject parent, int sortingOrder){
        //create square game object as child of delay
        GameObject square = new GameObject("Square");
        square.transform.parent = parent.transform;
        SpriteRenderer renderer = square.AddComponent<SpriteRenderer>();

        //set size of square
        square.transform.localScale = new Vector3(length, 100, 1);

        //set color of square
        renderer.color = c;
        renderer.sprite = squareSprite;

        //set local position of square
        square.transform.localPosition = new Vector3(-0.5f,0,0);
        
        square.transform.rotation = parent.gameObject.transform.rotation;

        renderer.sortingOrder = sortingOrder;
        return square; 
    }
}
