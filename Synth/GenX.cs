
// ChucK/STK source code to be ported.

//-----------------------------------------------------------------------------
// file: ugen_genX.cpp
// desc: thought it would be a good way to learn the fascinating innards of
//       ChucK by porting some of the classic lookup table functions and adding
//       a few new ones that might be of use.
//       mostly ported from RTcmix (all by WarpTable)
//
// author: Dan Trueman (dtrueman.princeton.edu)
// date: Winter 2007
//-----------------------------------------------------------------------------
// for member data offset
static uint genX_offset_data = 0;
// for internal usage
static void _transition( t_CKDOUBLE a, t_CKDOUBLE alpha, t_CKDOUBLE b,
                         t_CKINT n, t_CKDOUBLE * output );



//-----------------------------------------------------------------------------
// name: genX_query()
// desc: ...
//-----------------------------------------------------------------------------
DLL_QUERY genX_query( Chuck_DL_Query * QUERY )
{
    // srate
    g_srate = QUERY->srate;
    // get the env
    Chuck_Env * env = QUERY->env();
    std::string doc;
    Chuck_DL_Func * func = NULL;

    doc = "Ported from rtcmix. See <a href=\"http://www.music.columbia.edu/cmix/makegens.html\">\
    http://www.music.columbia.edu/cmix/makegens.html</a> \
    for more information on the GenX family of UGens. Currently coefficients past \
    the 100th are ignored.\
    \
    Lookup can either be done using the lookup() function, or by driving the \
    table with an input UGen, typically a Phasor. For an input signal between \
    [ -1, 1 ], using the absolute value for [ -1, 0 ), GenX will output the \
    table value indexed by the current input.";

    //---------------------------------------------------------------------
    // init as base class: genX
    //---------------------------------------------------------------------
    if( !type_engine_import_ugen_begin( env, "GenX", "UGen", env->global(),
                                        genX_ctor, genX_dtor, genX_tick, genX_pmsg,
                                        doc.c_str() ) )
        return FALSE;

    // add member variable
    genX_offset_data = type_engine_import_mvar( env, "int", "@GenX_data", FALSE );
    if( genX_offset_data == CK_INVALID_OFFSET ) goto error;

    func = make_new_mfun( "float", "lookup", genX_lookup ); //lookup table value
    func->add_arg( "float", "which" );
    func->doc = "Returns lookup table value at index i [ -1, 1 ]. Absolute value is used in the range [ -1, 0 ).";
    if( !type_engine_import_mfun( env, func ) ) goto error;

    func = make_new_mfun( "float[]", "coefs", genX_coeffs ); //load table
    func->add_arg( "float", "v[]" );
    func->doc = "Set lookup table coefficients. Meaning is dependent on subclass.";
    if( !type_engine_import_mfun( env, func ) ) goto error;

    // end the class import
    type_engine_import_class_end( env );


    //---------------------------------------------------------------------
    // gen5
    //---------------------------------------------------------------------
    doc = "Constructs a lookup table composed of sequential exponential curves. "
          "For a table with N curves, starting value of y', and value yn for lookup "
          "index xn, set the coefficients to [ y', y0, x0, ..., yN-1, xN-1 ]. "
          "Note that there must be an odd number of coefficients. "
          "If an even number of coefficients is specified, behavior is undefined. "
          "The sum of xn for 0 &le; n < N must be 1. yn = 0 is approximated as 0.000001 "
          "to avoid strange results arising from the nature of exponential curves.";

    if( !type_engine_import_ugen_begin( env, "Gen5", "GenX", env->global(),
                                        NULL, NULL, genX_tick, NULL, doc.c_str() ) )
        return FALSE;

    func = make_new_mfun( "float[]", "coefs", gen5_coeffs ); //load table
    func->add_arg( "float", "v[]" );
    func->doc = "Set lookup table coefficients.";
    if( !type_engine_import_mfun( env, func ) ) goto error;

    // end the class import
    type_engine_import_class_end( env );



    //---------------------------------------------------------------------
    // gen7
    //---------------------------------------------------------------------
    doc = "Constructs a lookup table composed of sequential line segments. "
          "For a table with N lines, starting value of y', and value yn for lookup"
          "index xn, set the coefficients to [ y', y0, x0, ..., yN-1, xN-1 ]. "
          "Note that there must be an odd number of coefficients. "
          "If an even number of coefficients is specified, behavior is undefined. "
          "The sum of xn for 0 &le; n < N must be 1.";

    if( !type_engine_import_ugen_begin( env, "Gen7", "GenX", env->global(),
                                        NULL, NULL, genX_tick, NULL, doc.c_str() ) )
        return FALSE;

    func = make_new_mfun( "float[]", "coefs", gen7_coeffs ); //load table
    func->add_arg( "float", "v[]" );
    func->doc = "Set lookup table coefficients.";
    if( !type_engine_import_mfun( env, func ) ) goto error;

    // end the class import
    type_engine_import_class_end( env );


    //---------------------------------------------------------------------
    // gen9
    //---------------------------------------------------------------------
    doc = "Constructs a lookup table of partials with specified amplitudes, "
          "phases, and harmonic ratios to the fundamental. "
          "Coefficients are specified in triplets of [ ratio, amplitude, phase ] "
          "arranged in a single linear array.";

    if( !type_engine_import_ugen_begin( env, "Gen9", "GenX", env->global(),
                                        NULL, NULL, genX_tick, NULL, doc.c_str() ) )
        return FALSE;

    func = make_new_mfun( "float[]", "coefs", gen9_coeffs ); //load table
    func->add_arg( "float", "v[]" );
    func->doc = "Set lookup table coefficients.";
    if( !type_engine_import_mfun( env, func ) ) goto error;

    // end the class import
    type_engine_import_class_end( env );



    //---------------------------------------------------------------------
    // gen10
    //---------------------------------------------------------------------
    doc = "Constructs a lookup table of harmonic partials with specified "
          "amplitudes. The amplitude of partial n is specified by the nth element of "
          "the coefficients. For example, setting coefs to [ 1 ] will produce a sine wave.";

    if( !type_engine_import_ugen_begin( env, "Gen10", "GenX", env->global(),
                                        NULL, NULL, genX_tick, NULL, doc.c_str() ) )
        return FALSE;

    func = make_new_mfun( "float[]", "coefs", gen10_coeffs ); //load table
    func->add_arg( "float", "v[]" );
    func->doc = "Set lookup table coefficients.";
    if( !type_engine_import_mfun( env, func ) ) goto error;

    // end the class import
    type_engine_import_class_end( env );


    //---------------------------------------------------------------------
    // gen17
    //---------------------------------------------------------------------
    doc = "Constructs a Chebyshev polynomial wavetable with harmonic partials "
          "of specified weights. The weight of partial n is specified by the nth "
          "element of the coefficients. Primarily used for waveshaping, driven by a "
          "SinOsc instead of a Phasor. See "
          "<a href=\"http://crca.ucsd.edu/~msp/techniques/v0.08/book-html/node74.html\">"
          "http://crca.ucsd.edu/~msp/techniques/v0.08/book-html/node74.html</a> and "
          "<a href=\"http://en.wikipedia.org/wiki/Distortion_synthesis\">"
          "http://en.wikipedia.org/wiki/Distortion_synthesis</a> for more information.";

    if( !type_engine_import_ugen_begin( env, "Gen17", "GenX", env->global(),
                                        NULL, NULL, genX_tick, NULL, doc.c_str() ) )
        return FALSE;

    func = make_new_mfun( "float[]", "coefs", gen17_coeffs ); //load table
    func->add_arg( "float", "v[]" );
    func->doc = "Set lookup table coefficients.";
    if( !type_engine_import_mfun( env, func ) ) goto error;

    // end the class import
    type_engine_import_class_end( env );

    //---------------------------------------------------------------------
    // Curve
    //---------------------------------------------------------------------
    doc = "Constructs a wavetable composed of segments of variable times, "
          "values, and curvatures. Coefficients are specified as a single linear "
          "array of triplets of [ time, value, curvature ] followed by a final duple "
          "of [ time, value ] to specify the final value of the table. time values "
          "are expressed in unitless, ascending values. For curvature equal to 0, "
          "the segment is a line; for curvature less than 0, the segment is a convex "
          "curve; for curvature greater than 0, the segment is a concave curve.";

    if( !type_engine_import_ugen_begin( env, "CurveTable", "GenX", env->global(),
                                        NULL, NULL, genX_tick, NULL, doc.c_str() ) )
        return FALSE;

    func = make_new_mfun( "float[]", "coefs", curve_coeffs ); //load table
    func->add_arg( "float", "v[]" );
    func->doc = "Set lookup table coefficients.";
    if( !type_engine_import_mfun( env, func ) ) goto error;

    // end the class import
    type_engine_import_class_end( env );

    //---------------------------------------------------------------------
    // Warp
    //---------------------------------------------------------------------
    doc = "";

    if( !type_engine_import_ugen_begin( env, "WarpTable", "GenX", env->global(),
                                        NULL, NULL, genX_tick, NULL, doc.c_str() ) )
        return FALSE;

    func = make_new_mfun( "float[]", "coefs", warp_coeffs ); //load table
    func->add_arg( "float", "v[]" );
    func->doc = "Set lookup table coefficients.";
    if( !type_engine_import_mfun( env, func ) ) goto error;

    /*
    func = make_new_mfun( "float", "coefs", warp_coeffs ); //load table
    func->add_arg( "float", "asym" );
    func->add_arg( "float", "sym" );
    if( !type_engine_import_mfun( env, func ) ) goto error;
    */

    // end the class import
    type_engine_import_class_end( env );


    return TRUE;

    error:

    // end the class import
    type_engine_import_class_end( env );

    return FALSE;
}




//-----------------------------------------------------------------------------
// name: struct genX_Data
// desc: ...
//-----------------------------------------------------------------------------
#define genX_tableSize 4096
#define genX_MAX_COEFFS 100

struct genX_Data
{
    uint genX_type;
    t_CKDOUBLE genX_table[genX_tableSize];
    // gewang: was int
    t_CKINT sync;
    uint srate;
    double xtemp;
    t_CKDOUBLE coeffs[genX_MAX_COEFFS];

    genX_Data()
    {
        //initialize data here
        sync        = 0;
        srate       = g_srate;

        t_CKINT i;
        for( i=0; i<genX_MAX_COEFFS; i++ ) coeffs[i] = 0.;
        for( i=0; i<genX_tableSize; i++ ) genX_table[i] = 0.;
    }
};




//-----------------------------------------------------------------------------
// name: genX_ctor()
// desc: ...
//-----------------------------------------------------------------------------
CK_DLL_CTOR( genX_ctor )
{
    genX_Data * d = new genX_Data;
    Chuck_DL_Return r;
    // return data to be used later
    OBJ_MEMBER_UINT(SELF, genX_offset_data) = (uint)d;
    //gen10_coeffs( SELF, &(d->xtemp), &r );
}




//-----------------------------------------------------------------------------
// name: genX_dtor()
// desc: ...
//-----------------------------------------------------------------------------
CK_DLL_DTOR( genX_dtor )
{
    // get the data
    genX_Data * data = (genX_Data *)OBJ_MEMBER_UINT(SELF, genX_offset_data );
    // delete
    SAFE_DELETE(data);
    // set to NULL
    OBJ_MEMBER_UINT(SELF, genX_offset_data) = 0;
}




//-----------------------------------------------------------------------------
// name: genX_tick()
// desc: ...
//-----------------------------------------------------------------------------
CK_DLL_TICK( genX_tick )
{
    // get the data
    genX_Data * d = (genX_Data *)OBJ_MEMBER_UINT(SELF, genX_offset_data );
    Chuck_UGen * ugen = (Chuck_UGen *)SELF;
//    t_CKBOOL inc_phase = TRUE;

    t_CKDOUBLE in_index = 0.0;
    t_CKDOUBLE scaled_index = 0.0;
    t_CKDOUBLE alpha = 0.0, omAlpha = 0.0;
    uint lowIndex = 0, hiIndex = 0;
    t_CKDOUBLE outvalue = 0.0;

    // if input
    if( ugen->m_num_src )
    {
        in_index = in;
        // gewang: moved this to here
        if( in_index < 0. ) in_index = -in_index;
        //scaled_index = (in_index + 1.) * 0.5 * genX_tableSize; //drive with oscillator, [-1, 1]
        scaled_index = in_index * genX_tableSize; //drive with phasor [0, 1]
    }
    else
    {
        scaled_index = 0.;
    }

    // set up interpolation parameters
    lowIndex = (uint)scaled_index;
    hiIndex = lowIndex + 1;
    alpha = scaled_index - lowIndex;
    omAlpha = 1. - alpha;

    // check table index ranges
    while(lowIndex >= genX_tableSize) lowIndex -= genX_tableSize;
    while(hiIndex >= genX_tableSize) hiIndex -= genX_tableSize;

    // could just call
    // outvalue = genX_lookup(in_index);?

    // calculate output value with linear interpolation
    outvalue = d->genX_table[lowIndex]*omAlpha + d->genX_table[hiIndex]*alpha;

    // set output
    *out = (SAMPLE)outvalue;

    return TRUE;
}


//-----------------------------------------------------------------------------
// name: genX_lookup()
// desc: lookup call for all gens
//-----------------------------------------------------------------------------
CK_DLL_CTRL( genX_lookup )
{
    // get the data
    genX_Data * d = (genX_Data *)OBJ_MEMBER_UINT(SELF, genX_offset_data );

    double in_index;
    double scaled_index;
    double alpha, omAlpha;
    uint lowIndex, hiIndex;
    double outvalue;

    in_index = GET_NEXT_FLOAT(ARGS);

    // gewang: moved to here
    if (in_index < 0.) in_index = -in_index;
    scaled_index = in_index * (genX_tableSize - 1); //drive with phasor [0, 1]

    //set up interpolation parameters
    lowIndex = (uint)scaled_index;
    hiIndex = lowIndex + 1;
    alpha = scaled_index - lowIndex;
    omAlpha = 1. - alpha;

    //check table index ranges
    while(lowIndex >= genX_tableSize) lowIndex -= genX_tableSize;
    while(hiIndex >= genX_tableSize) hiIndex -= genX_tableSize;

    //calculate output value with linear interpolation
    outvalue = d->genX_table[lowIndex]*omAlpha + d->genX_table[hiIndex]*alpha;

    RETURN->v_float = (double)outvalue;

}


//-----------------------------------------------------------------------------
// name: gen5_coeffs()
// desc: setup table for gen5
//-----------------------------------------------------------------------------
CK_DLL_CTRL( gen5_coeffs )
{
    // get data
    genX_Data * d = (genX_Data *)OBJ_MEMBER_UINT(SELF, genX_offset_data);
    t_CKINT i = 0, j, k, l, size;
    double wmax, xmax=0.0, c, amp2, amp1, coeffs[genX_MAX_COEFFS];

    Chuck_Array8 * in_args = (Chuck_Array8 *)GET_CK_OBJECT(ARGS);

    // CK_FPRINTF_STDOUT( "calling gen10coeffs, %d\n", weights );
    if(in_args == 0) return;
    size = in_args->size();
    if(size >= genX_MAX_COEFFS) size = genX_MAX_COEFFS - 1;

    double v;
    for(uint ii = 0; ii<size; ii++)
    {
        in_args->get(ii, &v);
        // CK_FPRINTF_STDOUT( "weight %d = %f...\n", ii, v );
        coeffs[ii] = v;
    }

    amp2 = coeffs[0];
    if (amp2 <= 0.0) amp2 = 0.000001;
    for(k = 1; k < size; k += 2)
    {
        amp1 = amp2;
        amp2 = coeffs[k+1];
        if (amp2 <= 0.0) amp2 = 0.000001;
        j = i + 1;
        d->genX_table[i] = amp1;
        c = (double) pow((amp2/amp1),(1./(coeffs[k]*genX_tableSize)));
        i = (t_CKINT)((j - 1) + coeffs[k]*genX_tableSize);
        for(l = j; l < i; l++)
        {
            if(l < genX_tableSize)
                d->genX_table[l] = d->genX_table[l-1] * c;
        }
    }

    for(j = 0; j < genX_tableSize; j++)
    {
        if ((wmax = fabs(d->genX_table[j])) > xmax) xmax = wmax;
        // CK_FPRINTF_STDOUT( "table current = %f\n", wmax);
    }
    // CK_FPRINTF_STDOUT( "table max = %f\n", xmax);
    for(j = 0; j < genX_tableSize; j++)
    {
        d->genX_table[j] /= xmax;
    }

    // return
    RETURN->v_object = in_args;
}


//-----------------------------------------------------------------------------
// name: gen7_coeffs()
// desc: setup table for gen7
//-----------------------------------------------------------------------------
CK_DLL_CTRL( gen7_coeffs )
{
    // get data
    genX_Data * d = (genX_Data *)OBJ_MEMBER_UINT(SELF, genX_offset_data);
    t_CKINT i=0, j, k, l, size;
    double wmax, xmax = 0.0, amp2, amp1, coeffs[genX_MAX_COEFFS];

    Chuck_Array8 * in_args = (Chuck_Array8 *)GET_CK_OBJECT(ARGS);

    // CK_FPRINTF_STDOUT( "calling gen10coeffs, %d\n", weights );
    if(in_args == 0) return;
    size = in_args->size();
    if(size >= genX_MAX_COEFFS) size = genX_MAX_COEFFS - 1;

    double v;
    for(uint ii = 0; ii<size; ii++)
    {
        in_args->get(ii, &v);
        // CK_FPRINTF_STDOUT( "weight %d = %f...\n", ii, v );
        coeffs[ii] = v;
    }

    amp2 = coeffs[0];
    for (k = 1; k < size; k += 2)
    {
        amp1 = amp2;
        amp2 = coeffs[k + 1];
        j = i + 1;
        i = (t_CKINT)(j + coeffs[k]*genX_tableSize - 1);
        for (l = j; l <= i; l++)
        {
            if (l <= genX_tableSize)
                d->genX_table[l - 1] = amp1 +
                                       (amp2 - amp1) * (double) (l - j) / (i - j + 1);
        }
    }

    for(j = 0; j < genX_tableSize; j++)
    {
        if ((wmax = fabs(d->genX_table[j])) > xmax) xmax = wmax;
        // CK_FPRINTF_STDOUT( "table current = %f\n", wmax);
    }
    // CK_FPRINTF_STDOUT( "table max = %f\n", xmax);
    for(j = 0; j < genX_tableSize; j++)
    {
        d->genX_table[j] /= xmax;
    }

    // return
    RETURN->v_object = in_args;
}


//-----------------------------------------------------------------------------
// name: gen9_coeffs()
// desc: setup table for gen9
//-----------------------------------------------------------------------------
CK_DLL_CTRL( gen9_coeffs )
{
    // get data
    genX_Data * d = (genX_Data *)OBJ_MEMBER_UINT(SELF, genX_offset_data);
    t_CKINT i, j, size;
    t_CKDOUBLE wmax, xmax=0.0;
    double coeffs[genX_MAX_COEFFS];

    Chuck_Array8 * weights = (Chuck_Array8 *)GET_CK_OBJECT(ARGS);

    // CK_FPRINTF_STDOUT( "calling gen10coeffs, %d\n", weights );
    if(weights == 0) return;
    size = weights->size();
    if(size >= genX_MAX_COEFFS) size = genX_MAX_COEFFS - 1;


    double v;
    for(uint ii = 0; ii<size; ii++)
    {
        weights->get(ii, &v);
        // CK_FPRINTF_STDOUT( "weight %d = %f...\n", ii, v );
        coeffs[ii] = v;
    }

    for(j = size - 1; j > 0; j -= 3)
    {
        if(coeffs[j - 1] != 0)
        {
            for(i = 0; i < genX_tableSize; i++)
            {
                t_CKDOUBLE val = sin(TWO_PI * ((t_CKDOUBLE) i / ((t_CKDOUBLE) (genX_tableSize)
                                               / coeffs[j - 2]) + coeffs[j] / 360.));
                d->genX_table[i] += val * coeffs[j - 1];
            }
        }
    }

    for(j = 0; j < genX_tableSize; j++)
    {
        if ((wmax = fabs(d->genX_table[j])) > xmax) xmax = wmax;
        // CK_FPRINTF_STDOUT( "table current = %f\n", wmax);
    }
    // CK_FPRINTF_STDOUT( "table max = %f\n", xmax);
    for(j = 0; j < genX_tableSize; j++)
    {
        d->genX_table[j] /= xmax;
    }

    // return
    RETURN->v_object = weights;
}


//-----------------------------------------------------------------------------
// name: gen10_coeffs()
// desc: setup table for gen10
//-----------------------------------------------------------------------------
CK_DLL_CTRL( gen10_coeffs )
{
    // get data
    genX_Data * d = (genX_Data *)OBJ_MEMBER_UINT(SELF, genX_offset_data);
    t_CKINT i, j, size;
    t_CKDOUBLE wmax, xmax=0.0;

    Chuck_Array8 * weights = (Chuck_Array8 *)GET_CK_OBJECT(ARGS);

    // CK_FPRINTF_STDOUT( "calling gen10coeffs, %d\n", weights );
    if(weights==0) return;
    size = weights->size();
    if(size >= genX_MAX_COEFFS) size = genX_MAX_COEFFS - 1;

    double v;
    for(uint ii = 0; ii<size; ii++)
    {
        weights->get(ii, &v);
        // CK_FPRINTF_STDOUT( "weight %d = %f...\n", ii, v );
        d->coeffs[ii] = v;
    }

    j = genX_MAX_COEFFS;
    while (j--)
    {
        if (d->coeffs[j] != 0)
        {
            for (i = 0; i < genX_tableSize; i++)
            {
                t_CKDOUBLE val = (t_CKDOUBLE) (TWO_PI * (t_CKDOUBLE) i / (genX_tableSize / (j + 1)));
                d->genX_table[i] += sin(val) * d->coeffs[j];
            }
        }
    }

    for(j = 0; j < genX_tableSize; j++)
    {
        if ((wmax = fabs(d->genX_table[j])) > xmax) xmax = wmax;
        // CK_FPRINTF_STDOUT( "table current = %f\n", wmax);
    }

    // CK_FPRINTF_STDOUT( "table max = %f\n", xmax);
    for(j = 0; j < genX_tableSize; j++)
    {
        d->genX_table[j] /= xmax;
    }

    // return
    RETURN->v_object = weights;
}


//-----------------------------------------------------------------------------
// name: gen17_coeffs()
// desc: setup table for gen17
//-----------------------------------------------------------------------------
CK_DLL_CTRL( gen17_coeffs )
{
    // get data
    genX_Data * d = (genX_Data *)OBJ_MEMBER_UINT(SELF, genX_offset_data);
    t_CKINT i, j, size;
    t_CKDOUBLE Tn, Tn1, Tn2, dg, x, wmax = 0.0, xmax = 0.0;
    double coeffs[genX_MAX_COEFFS];

    Chuck_Array8 * weights = (Chuck_Array8 *)GET_CK_OBJECT(ARGS);

    // CK_FPRINTF_STDOUT( "calling gen17coeffs, %d\n", weights );
    if(weights == 0) return;
    size = weights->size();
    if(size >= genX_MAX_COEFFS) size = genX_MAX_COEFFS - 1;

    dg = (t_CKDOUBLE) (genX_tableSize / 2. - .5);

    double v;
    for(uint ii = 0; ii<size; ii++)
    {
        weights->get(ii, &v);
        // CK_FPRINTF_STDOUT( "weight %d = %f...\n", ii, v );
        coeffs[ii] = v;
    }

    for (i = 0; i < genX_tableSize; i++)
    {
        x = (t_CKDOUBLE)(i / dg - 1.);
        d->genX_table[i] = 0.0;
        Tn1 = 1.0;
        Tn = x;
        for (j = 0; j < size; j++)
        {
            d->genX_table[i] = coeffs[j] * Tn + d->genX_table[i];
            Tn2 = Tn1;
            Tn1 = Tn;
            Tn = 2.0 * x * Tn1 - Tn2;
        }
    }

    for(j = 0; j < genX_tableSize; j++)
    {
        if ((wmax = fabs(d->genX_table[j])) > xmax) xmax = wmax;
        // CK_FPRINTF_STDOUT( "table current = %f\n", wmax);
    }
    // CK_FPRINTF_STDOUT( "table max = %f\n", xmax);
    for(j = 0; j < genX_tableSize; j++)
    {
        d->genX_table[j] /= xmax;
        // CK_FPRINTF_STDOUT( "table current = %f\n", d->genX_table[j]);
    }

    // return
    RETURN->v_object = weights;
}


//-----------------------------------------------------------------------------
// name: curve_coeffs()
// desc: setup table for Curve
// ported from RTcmix
//-----------------------------------------------------------------------------
#define MAX_CURVE_PTS 256
CK_DLL_CTRL( curve_coeffs )
{
    // get data
    genX_Data * d = (genX_Data *)OBJ_MEMBER_UINT(SELF, genX_offset_data);
    t_CKINT i, points, nargs, seglen = 0, len = genX_tableSize;
    t_CKDOUBLE factor, *ptr;//, xmax=0.0;
    t_CKDOUBLE time[MAX_CURVE_PTS], value[MAX_CURVE_PTS], alpha[MAX_CURVE_PTS];
    double coeffs[genX_MAX_COEFFS];
    uint ii = 0;
    double v = 0.0;

    Chuck_Array8 * weights = (Chuck_Array8 *)GET_CK_OBJECT(ARGS);

    // CK_FPRINTF_STDOUT( "calling gen17coeffs, %d\n", weights );
    if(weights==0) goto done;

    nargs = weights->size();
    if (nargs < 5 || (nargs % 3) != 2)      // check number of args
    {
        CK_FPRINTF_STDERR( "[chuck](via CurveTable): usage: \n size, time1, value1, curvature1, [ timeN-1, valueN-1, curvatureN-1, ] timeN, valueN)\n" );
        goto done;
    }
    if ((nargs / 3) + 1 > MAX_CURVE_PTS)
    {
        CK_FPRINTF_STDERR( "[chuck](via CurveTable): too many arguments.\n" );
        goto done;
    }

    for(ii = 0; ii<nargs; ii++)
    {
        weights->get(ii, &v);
        // CK_FPRINTF_STDOUT( "weight %d = %f...\n", ii, v );
        coeffs[ii] = v;
    }

    if (coeffs[0] != 0.0)
    {
        CK_FPRINTF_STDERR( "[chuck](via CurveTable): first time must be zero.\n" );
        goto done;
    }

    for (i = points = 0; i < nargs; points++)
    {
        time[points] = (t_CKDOUBLE) coeffs[i++];
        if (points > 0 && time[points] < time[points - 1])
            goto time_err;
        value[points] = (t_CKDOUBLE) coeffs[i++];
        if (i < nargs)
            alpha[points] = (t_CKDOUBLE) coeffs[i++];
    }

    factor = (t_CKDOUBLE) (len - 1) / time[points - 1];
    for (i = 0; i < points; i++)
        time[i] *= factor;

    ptr = d->genX_table;
    for (i = 0; i < points - 1; i++)
    {
        seglen = (t_CKINT) (floor(time[i + 1] + 0.5) - floor(time[i] + 0.5)) + 1;
        _transition(value[i], alpha[i], value[i + 1], seglen, ptr);
        ptr += seglen - 1;
    }

    done:
    // return
    RETURN->v_object = weights;

    return;

    time_err:
    CK_FPRINTF_STDERR( "[chuck](via CurveTable): times must be in ascending order.\n" );

    // return
    RETURN->v_object = weights;

    return;
}


static void _transition(t_CKDOUBLE a, t_CKDOUBLE alpha, t_CKDOUBLE b, t_CKINT n, t_CKDOUBLE *output)
{
    t_CKINT  i;
    t_CKDOUBLE delta, interval = 0.0;

    delta = b - a;

    if (n <= 1)
    {
        //warn("maketable (curve)", "Trying to transition over 1 array slot; "
        //                                "time between points is too short");
        *output = a;
        return;
    }
    interval = 1.0 / (n - 1.0);

    if (alpha != 0.0)
    {
        t_CKDOUBLE denom = 1.0 / (1.0 - exp(alpha));
        for (i = 0; i < n; i++)
            *output++ = (a + delta
                         * (1.0 - exp((double) i * alpha * interval)) * denom);
    }
    else
        for (i = 0; i < n; i++)
            *output++ = a + delta * i * interval;
}


//-----------------------------------------------------------------------------
// name: warp_coeffs()
// desc: setup table for warp
//-----------------------------------------------------------------------------
CK_DLL_CTRL( warp_coeffs )
{
    // get data
    genX_Data * d = (genX_Data *)OBJ_MEMBER_UINT(SELF, genX_offset_data);
    t_CKINT i = 0;

    double k_asym = 1.;
    double k_sym  = 1.;

    // gewang:
    Chuck_Array8 * weights = (Chuck_Array8 *)GET_CK_OBJECT(ARGS);
    // check for size
    if( weights->size() != 2 )
    {
        // error
        CK_FPRINTF_STDERR( "[chuck](via WarpTable): expects array of exactly 2 elements.\n" );
        goto done;
    }

    weights->get( 0, &k_asym ); // (t_CKDOUBLE) GET_NEXT_FLOAT(ARGS);
    weights->get( 1, &k_sym ); // (t_CKDOUBLE) GET_NEXT_FLOAT(ARGS);

    for (i = 0; i < genX_tableSize; i++)
    {
        t_CKDOUBLE inval = (t_CKDOUBLE) i/(genX_tableSize - 1);
        if(k_asym == 1 && k_sym == 1)
        {
            d->genX_table[i]    = inval;
        }
        else if(k_sym == 1)
        {
            d->genX_table[i]    = _asymwarp(inval, k_asym);
        }
        else if(k_asym == 1)
        {
            d->genX_table[i]    = _symwarp(inval, k_sym);
        }
        else
        {
            inval               = _asymwarp(inval, k_asym);
            d->genX_table[i]    = _symwarp(inval, k_sym);
        }
        // CK_FPRINTF_STDOUT( "table %d = %f\n", i, d->genX_table[i] );
    }

    done:

    // return
    RETURN->v_object = weights;
}


t_CKDOUBLE _asymwarp(t_CKDOUBLE inval, t_CKDOUBLE k)
{
    return (pow(k, inval) - 1.) / (k - 1.);
}


t_CKDOUBLE _symwarp(t_CKDOUBLE inval, t_CKDOUBLE k)
{
    t_CKDOUBLE sym_warped;
    if(inval >= 0.5)
    {
        sym_warped = pow(2.*inval - 1., 1./k);
        return (sym_warped + 1.) * 0.5;

    }
    inval = 1. - inval; // for S curve
    sym_warped = pow(2.*inval - 1., 1./k);
    sym_warped = (sym_warped + 1.) * 0.5;

    return 1. - sym_warped;
}


// also do RTcmix "spline" on a rainy day


//-----------------------------------------------------------------------------
// name: genX_pmsg()
// desc: ...
//-----------------------------------------------------------------------------
CK_DLL_PMSG( genX_pmsg )
{
    //genX_Data * d = (genX_Data *)OBJ_MEMBER_UINT(SELF, genX_offset_data );
    if( !strcmp( MSG, "print" ) )
    {
        // CK_FPRINTF_STDOUT( "genX:" );
        return TRUE;
    }

    // didn't handle
    return FALSE;
}


//-----------------------------------------------------------------------------
// name: genX_coeffs()
// desc: ...
//-----------------------------------------------------------------------------
CK_DLL_CTRL( genX_coeffs )
{
    // nope
    CK_FPRINTF_STDERR( "[chuck](via GenX): .coeffs called on abstract base class!\n" );

    // return
    RETURN->v_object = GET_NEXT_OBJECT(ARGS);

    return;
}
