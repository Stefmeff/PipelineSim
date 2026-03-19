using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/**
 * @brief Static utility for delay visualization squares and signal queue management.
 *
 * @details Each component with a propagation delay uses a signal queue (List of (BitToken, GameObject) tuples).
 * As signals enter a component, a colored square is created and animated across the delay visualizer bar.
 * When the signal exits, the square is returned to an object pool for reuse.
 *
 * The pool starts empty and grows organically as signals flow through components.
 * Once throughput stabilizes (especially in synchronous circuits), no new allocations occur —
 * squares are recycled between the pool and active signal queues.
 **/
public static class DelayHandler
{
    private static Texture2D texture = new Texture2D(1, 1);  /**< shared 1x1 white texture for all squares */
    private static Sprite squareSprite;                       /**< sprite created from the white texture */

    /**
     * @brief Object pool for delay visualization squares.
     *
     * @details Inactive GameObjects are pushed here when signals exit a component,
     * and popped for reuse when new signals enter. This avoids costly
     * GameObject.Instantiate/Destroy calls during simulation.
     **/
    private static Stack<GameObject> pool = new Stack<GameObject>();

    /**
     * @brief Static constructor — creates the shared white 1x1 sprite used by all delay squares.
     **/
    static DelayHandler()
    {
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        squareSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0, 0.5f));
    }


    /**
     * @brief Checks whether the next signal in the queue has completed its delay and is ready to be output.
     *
     * @details If the second signal's arrival time plus the delay has elapsed, the oldest square is
     * returned to the pool, removed from the queue, and the signal is returned with an updated timestamp.
     * Otherwise, the current (head) signal is returned unchanged.
     *
     * @param[in,out] signalQueue the component's signal queue (BitToken + visualization square pairs)
     * @param[in] delay the component's propagation delay in ticks
     * @param[in] tick the current simulation tick
     * @return the BitToken that should be set on the component's output pin
     **/
    public static BitToken CheckDelayQueue(List<SignalEntry> signalQueue, int delay, int tick){
        if (signalQueue.Count > 1)
        {

            BitToken nextOut = signalQueue[1].token;
            int arrivalTime = nextOut.GetTime();

            if (arrivalTime + delay <= tick)
            {
                ReturnSquare(signalQueue[0].visual);
                signalQueue.RemoveAt(0);
                return nextOut.NewToken(arrivalTime + delay);
            }
        }
        return signalQueue[0].token;
    }

    /**
     * @brief Updates the visual size of each square in the signal queue to reflect
     * how far the signal has traveled through the delay.
     *
     * @details Each square's width is scaled proportionally: 0 at entry, 100 when
     * the delay is fully elapsed. If delay is 0, squares are drawn at full width.
     *
     * @param[in] signalQueue the component's signal queue
     * @param[in] delay the component's propagation delay in ticks
     * @param[in] tick the current simulation tick
     **/
    public static void DrawSquares(List<SignalEntry> signalQueue, int delay, int tick){
        foreach(SignalEntry t in signalQueue){
            if (t.visual == null) continue;
            if(delay == 0){
                t.visual.transform.localScale = new Vector3(100, 100, 1);
            }else{
                int passedTime = tick-t.token.GetTime();

                if(passedTime <= delay && passedTime >= 0){
                    float length = 100*passedTime/delay;
                    t.visual.transform.localScale = new Vector3(length, 100, 1);
                }
            }

        }
    }

    /**
     * @brief Gets a delay visualization square — either by reusing one from the pool
     * or by creating a new GameObject if the pool is empty.
     *
     * @details Pooled squares are reactivated and reparented to the given parent.
     * New squares get a SpriteRenderer with the shared white sprite.
     * Both are configured with the given color, size, and position.
     *
     * @param[in] length initial width of the square (0 for newly entering signals, 100 for init)
     * @param[in] c the signal's display color (from BitToken.ActiveColor())
     * @param[in] parent the delay visualizer bar GameObject to parent the square under
     * @param[in] sortingOrder sprite sorting order
     * @return the configured square GameObject
     **/
    public static GameObject NewSquare(float length, Color c, GameObject parent, int sortingOrder){
        GameObject square = null;
        SpriteRenderer renderer;

        // Try to reuse from pool — skip destroyed squares (stale after scene reload)
        while (pool.Count > 0 && square == null)
        {
            square = pool.Pop();
        }

        if (square != null)
        {
            // Reuse pooled square
            square.SetActive(true);
            square.transform.parent = parent.transform;
            renderer = square.GetComponent<SpriteRenderer>();
        }
        else
        {
            // Pool empty or all squares were destroyed — create new
            square = new GameObject("Square");
            square.transform.parent = parent.transform;
            renderer = square.AddComponent<SpriteRenderer>();
            renderer.sprite = squareSprite;
        }

        // Common: configure size, color, position and sorting for both cases
        square.transform.localScale = new Vector3(length, 100, 1);
        renderer.color = c;
        square.transform.localPosition = new Vector3(-0.5f,0,0);
        square.transform.rotation = parent.gameObject.transform.rotation;
        renderer.sortingOrder = sortingOrder;
        return square;
    }

    /**
     * @brief Returns a square to the object pool for reuse.
     *
     * @details The square is deactivated (stops rendering, zero CPU cost) and pushed
     * onto the pool stack. Null-safe — does nothing if square is null.
     *
     * @param[in] square the square GameObject to return
     **/
    public static void ReturnSquare(GameObject square){
        if (square == null) return;
        square.SetActive(false);
        pool.Push(square);
    }
}
