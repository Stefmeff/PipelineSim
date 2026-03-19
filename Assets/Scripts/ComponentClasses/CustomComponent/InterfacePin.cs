using Newtonsoft.Json;
using System;

/**
 * @brief Defines a single interface pin on a custom chip (input or output).
 *
 * @details Stored as part of a ChipDefinition. Each pin has a unique ID, a user-visible
 * name, a bit width, and an order index determining its vertical position on the chip body.
 **/
[Serializable]
public class InterfacePin
{
    [JsonProperty] public string id;    /**< unique identifier for this pin */
    [JsonProperty] public string name;  /**< user-visible pin label */
    [JsonProperty] public int width;    /**< bit width (1 = single-bit, n = n-bit bus) */
    [JsonProperty] public int order;    /**< vertical position index on the chip body */

    /**
     * @brief Default constructor — creates a 1-bit pin named "Pin".
     **/
    public InterfacePin()
    {
        id = Guid.NewGuid().ToString();
        name = "Pin";
        width = 1;
        order = 0;
    }

    /**
     * @brief Parameterized constructor.
     *
     * @param[in] name user-visible pin label
     * @param[in] width bit width
     * @param[in] order vertical position index
     **/
    public InterfacePin(string name, int width, int order)
    {
        id = Guid.NewGuid().ToString();
        this.name = name;
        this.width = width;
        this.order = order;
    }
}
