
using System;
using System.Collections.Generic;

namespace Nebulator.Synth
{
////// from ugen_xxx.* ///////


public class Mix : UGen
{
    public List<UGen> Inputs
    {
        get;
        set;
    } = new List<UGen>();

    public Mix() //TODOX these
    {
        // // multi
        // if( ugen->m_multi_chan_size )
        // {
        //     // set left
        //     OBJ_MEMBER_UINT(SELF, stereo_offset_left) = (uint)(ugen->m_multi_chan[0]);
        //     // set right
        //     OBJ_MEMBER_UINT(SELF, stereo_offset_right) = (uint)(ugen->m_multi_chan[1]);
        // }
        // else // mono
        // {
        //     // set left and right to self
        //     OBJ_MEMBER_UINT(SELF, stereo_offset_left) = (uint)ugen;
        //     OBJ_MEMBER_UINT(SELF, stereo_offset_right) = (uint)ugen;
        // }
    }

    public override double Sample(double din1, double din2)
    {
        return (din1 + din2) * Gain1;
    }
}

public class Pan : UGen
{
    // public double Pan1 { get; set; } = 0.0;
    // public double Pan2 { get; set; } = 0.0;

    public Pan()
    {
        // Chuck_UGen * ugen = (Chuck_UGen * )SELF;
        // Chuck_UGen * left = ugen->m_multi_chan[0];
        // Chuck_UGen * right = ugen->m_multi_chan[1];
        // // get arg
        // double pan = GET_CK_FLOAT(ARGS);
        // // clip it
        // if( pan < -1.0 ) pan = -1.0;
        // else if( pan > 1.0 ) pan = 1.0;
        // // set it
        // OBJ_MEMBER_FLOAT(SELF, stereo_offset_pan) = pan;
        // // pan it
        // left->m_pan = pan < 0.0 ? 1.0 : 1.0 - pan;
        // right->m_pan = pan > 0.0 ? 1.0 : 1.0 + pan;
    }

    public override double Sample(double din)
    {
        return din * Gain1; //TODOX stereo
    }
}
}