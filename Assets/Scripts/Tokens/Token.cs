using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Token
{
    private int size;
    private Color color;
    private BitToken[] bits;

    /**
    * Constructor of token object
    *
    * @param size of bit vector
    * @param Color of token
    **/
    public Token(int size, Color c){
        this.size = size;
        this.color = c;

        this.bits = new BitToken[size];

        for(int i = 0; i < size; i++){
            bits[i] = new BitToken(true,0,c);
        }
    }

    /**
    * Constructor of token object
    *
    * @param binary string containing bits of token (precondition: string only consists of 0 and 1)
    * @param Color of token
    **/
    public Token(string binary, Color c){
        this.color = c;
        this.size = binary.Length;
        this.bits = StringToArray(binary);
    }

    //precondition: string consists solely of binary numbers, e.g. "10110"
    public BitToken[] StringToArray(string binary){
        BitToken[] result = new BitToken[binary.Length];

        for (int i = 0; i < binary.Length; i++)
        {
            if (binary[i] == '1')
                result[i] = new BitToken(true,0,color);
            else if (binary[i] == '0')
                result[i] = new BitToken(false,0,color);
        }

        return result;
    }

    public BitToken GetBitAt(int index){
        if(index < size){
            return bits[index];
        }
        return null;
    }

}
