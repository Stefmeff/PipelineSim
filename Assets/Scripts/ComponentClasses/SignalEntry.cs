using UnityEngine;

/**
 * @brief Value type for signal queue entries, replacing Tuple<BitToken, GameObject>.
 *
 * @details Each component with a propagation delay maintains a signal queue tracking
 * signals traveling through it. This struct pairs the signal data (BitToken) with its
 * optional delay visualization square (GameObject). Using a struct instead of Tuple
 * avoids heap allocation and GC pressure on every signal transition.
 **/
public struct SignalEntry
{
    public BitToken token;   /**< the signal data (value, timestamp, color) */
    public GameObject visual; /**< the delay visualization square, or null if not visualized */

    public SignalEntry(BitToken token, GameObject visual)
    {
        this.token = token;
        this.visual = visual;
    }
}
