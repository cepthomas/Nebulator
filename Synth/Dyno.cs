
// ChucK/STK source code to be ported. TODON3

class Dyno_Data
{
    private:
    t_CKDUR ms; // changed 1.3.5.3 ge from const static

    public:
    t_CKFLOAT slopeAbove;
    t_CKFLOAT slopeBelow;
    t_CKFLOAT thresh;
    t_CKFLOAT rt;
    t_CKFLOAT at;
    t_CKFLOAT xd; //sidechain
    int externalSideInput; // use input signal or a ctrl signal for env
    t_CKFLOAT sideInput;   // the ctrl signal for the envelope

    int count; //diagnostic

    Dyno_Data( Chuck_VM * vm )
    {
        ms = vm->srate() / 1000.0;
        xd = 0.0;
        count = 0;
        sideInput = 0;
        limit();
    }
    ~Dyno_Data() {}

    void limit();
    void compress();
    void gate();
    void expand();
    void duck();

    //set the time constants for rt, at, and tav
    static t_CKFLOAT computeTimeConst(t_CKDUR t)
    {
        //AT = 1 - e ^ (-2.2T/t<AT)
        //as per chuck_type.cpp, T(sampling period) = 1.0
        return 1.0 - exp( -2.2 / t );
    }

    static t_CKDUR timeConstToDur(t_CKFLOAT x)
    {
        return -2.2 / log(1.0 - x);
    }

    //setters for timing constants
    void setAttackTime(t_CKDUR t);
    void setReleaseTime(t_CKDUR t);

    //other setters
    void setRatio(t_CKFLOAT newRatio);
    t_CKFLOAT getRatio();
};



//setters for the timing constants
void Dyno_Data::setAttackTime(t_CKDUR t)
{
    at = computeTimeConst(t);
}

void Dyno_Data::setReleaseTime(t_CKDUR t)
{
    rt = computeTimeConst(t);
}

void Dyno_Data::setRatio(t_CKFLOAT newRatio)
{
    this->slopeAbove = 1.0 / newRatio;
    this->slopeBelow = 1.0;
}

t_CKFLOAT Dyno_Data::getRatio()
{
    return this->slopeBelow / this->slopeAbove;
}

//TODO: come up with better/good presets?

//presets for the dynomics processor
void Dyno_Data::limit()
{
    slopeAbove = 0.1;   // 10:1 compression above thresh
    slopeBelow = 1.0;    // no compression below
    thresh = 0.5;
    at = computeTimeConst( 5.0 * ms );
    rt = computeTimeConst( 300.0 * ms );
    externalSideInput = 0;
}

void Dyno_Data::compress()
{
    slopeAbove = 0.5;   // 2:1 compression
    slopeBelow = 1.0;
    thresh = 0.5;
    at = computeTimeConst( 5.0 * ms );
    rt = computeTimeConst( 500.0 * ms );
    externalSideInput = 0;
}

void Dyno_Data::gate()
{
    slopeAbove = 1.0;
    slopeBelow = 100000000; // infinity (more or less)
    thresh = 0.1;
    at = computeTimeConst( 11.0 * ms );
    rt = computeTimeConst( 100.0 * ms );
    externalSideInput = 0;
}

void Dyno_Data::expand()
{
    slopeAbove = 2.0;    // 1:2 expansion
    slopeBelow = 1.0;
    thresh = 0.5;
    at = computeTimeConst( 20.0 * ms );
    rt = computeTimeConst( 400.0 * ms );
    externalSideInput = 0;
}

void Dyno_Data::duck()
{
    slopeAbove = 0.5;    // when sideInput rises above thresh, gain starts going
    slopeBelow = 1.0;    // down. it'll drop more as sideInput gets louder.
    thresh = 0.1;
    at = computeTimeConst( 10.0 * ms );
    rt = computeTimeConst( 1000.0 * ms );
    externalSideInput = 1;
}


// controls for the preset modes
CK_DLL_CTRL( dyno_ctrl_limit )
{
    Dyno_Data * d = ( Dyno_Data * )OBJ_MEMBER_UINT(SELF, dyno_offset_data);
    d->limit();
}

CK_DLL_CTRL( dyno_ctrl_compress )
{
    Dyno_Data * d = ( Dyno_Data * )OBJ_MEMBER_UINT(SELF, dyno_offset_data);
    d->compress();
}

CK_DLL_CTRL( dyno_ctrl_gate )
{
    Dyno_Data * d = ( Dyno_Data * )OBJ_MEMBER_UINT(SELF, dyno_offset_data);
    d->gate();
}

CK_DLL_CTRL( dyno_ctrl_expand )
{
    Dyno_Data * d = ( Dyno_Data * )OBJ_MEMBER_UINT(SELF, dyno_offset_data);
    d->expand();
}

CK_DLL_CTRL( dyno_ctrl_duck )
{
    Dyno_Data * d = ( Dyno_Data * )OBJ_MEMBER_UINT(SELF, dyno_offset_data);
    d->duck();
}

//additional controls: thresh
CK_DLL_CTRL( dyno_ctrl_thresh )
{
    Dyno_Data * d = ( Dyno_Data * )OBJ_MEMBER_UINT(SELF, dyno_offset_data);
    d->thresh = GET_CK_FLOAT(ARGS);
    RETURN->v_float = (t_CKFLOAT)d->thresh;
}

CK_DLL_CGET( dyno_cget_thresh )
{
    Dyno_Data * d = ( Dyno_Data * )OBJ_MEMBER_UINT(SELF, dyno_offset_data);
    RETURN->v_float = (t_CKFLOAT)d->thresh;
}

//additional controls: attackTime
CK_DLL_CTRL( dyno_ctrl_attackTime )
{
    Dyno_Data * d = ( Dyno_Data * )OBJ_MEMBER_UINT(SELF, dyno_offset_data);
    d->setAttackTime( GET_CK_FLOAT(ARGS) );
    RETURN->v_dur = d->timeConstToDur(d->at);
}

CK_DLL_CGET( dyno_cget_attackTime )
{
    Dyno_Data * d = ( Dyno_Data * )OBJ_MEMBER_UINT(SELF, dyno_offset_data);
    RETURN->v_dur = d->timeConstToDur(d->at);
}

//additional controls: releaseTime
CK_DLL_CTRL( dyno_ctrl_releaseTime )
{
    Dyno_Data * d = ( Dyno_Data * )OBJ_MEMBER_UINT(SELF, dyno_offset_data);
    d->setReleaseTime( GET_CK_FLOAT(ARGS) );
    RETURN->v_dur = d->timeConstToDur(d->rt);
}

CK_DLL_CGET( dyno_cget_releaseTime )
{
    Dyno_Data * d = ( Dyno_Data * )OBJ_MEMBER_UINT(SELF, dyno_offset_data);
    RETURN->v_dur = d->timeConstToDur(d->rt);
}

//additional controls: ratio
CK_DLL_CTRL( dyno_ctrl_ratio )
{
    Dyno_Data * d = ( Dyno_Data * )OBJ_MEMBER_UINT(SELF, dyno_offset_data);
    d->setRatio( GET_CK_FLOAT(ARGS) );
    RETURN->v_float = d->getRatio();
}

CK_DLL_CGET( dyno_cget_ratio )
{
    Dyno_Data * d = ( Dyno_Data * )OBJ_MEMBER_UINT(SELF, dyno_offset_data);
    RETURN->v_float = d->getRatio();
}

//additional controls: slopeBelow
CK_DLL_CTRL( dyno_ctrl_slopeBelow )
{
    Dyno_Data * d = ( Dyno_Data * )OBJ_MEMBER_UINT(SELF, dyno_offset_data);
    d->slopeBelow = GET_CK_FLOAT(ARGS);

    RETURN->v_float = d->slopeBelow;
}

CK_DLL_CGET( dyno_cget_slopeBelow )
{
    Dyno_Data * d = ( Dyno_Data * )OBJ_MEMBER_UINT(SELF, dyno_offset_data);
    RETURN->v_float = d->slopeBelow;
}

//additional controls: slopeAbove
CK_DLL_CTRL( dyno_ctrl_slopeAbove )
{
    Dyno_Data * d = ( Dyno_Data * )OBJ_MEMBER_UINT(SELF, dyno_offset_data);
    d->slopeAbove = GET_CK_FLOAT(ARGS);

    RETURN->v_float = d->slopeAbove;
}

CK_DLL_CGET( dyno_cget_slopeAbove )
{
    Dyno_Data * d = ( Dyno_Data * )OBJ_MEMBER_UINT(SELF, dyno_offset_data);
    RETURN->v_float = d->slopeAbove;
}

//additional controls: sideInput
CK_DLL_CTRL( dyno_ctrl_sideInput )
{
    Dyno_Data * d = ( Dyno_Data * )OBJ_MEMBER_UINT(SELF, dyno_offset_data);
    d->sideInput = GET_CK_FLOAT(ARGS);

    RETURN->v_float = d->sideInput;
}

CK_DLL_CGET( dyno_cget_sideInput )
{
    Dyno_Data * d = ( Dyno_Data * )OBJ_MEMBER_UINT(SELF, dyno_offset_data);
    RETURN->v_float = d->sideInput;
}

//additional controls: externalSideInput
CK_DLL_CTRL( dyno_ctrl_externalSideInput )
{
    Dyno_Data * d = ( Dyno_Data * )OBJ_MEMBER_UINT(SELF, dyno_offset_data);
    d->externalSideInput = GET_CK_INT(ARGS);

    RETURN->v_int = d->externalSideInput;
}

CK_DLL_CGET( dyno_cget_externalSideInput )
{
    Dyno_Data * d = ( Dyno_Data * )OBJ_MEMBER_UINT(SELF, dyno_offset_data);
    RETURN->v_int = d->externalSideInput;
}

//constructor
CK_DLL_CTOR( dyno_ctor )
{
    OBJ_MEMBER_UINT(SELF, dyno_offset_data) = (t_CKUINT)new Dyno_Data( SHRED->vm_ref );
}

CK_DLL_DTOR( dyno_dtor )
{
    delete (Dyno_Data *)OBJ_MEMBER_UINT(SELF, dyno_offset_data);
    OBJ_MEMBER_UINT(SELF, dyno_offset_data) = 0;
}

// recomputes envelope, determines how the current amp envelope compares with
// thresh, applies the appropriate new slope depending on how far above/below
// the threshold the current envelope is.
CK_DLL_TICK( dyno_tick )
{
    Dyno_Data * d = ( Dyno_Data * )OBJ_MEMBER_UINT(SELF, dyno_offset_data);

    // only change sideInput if we're not using an external ctrl signal.
    // otherwise we'll just use whatever the user sent us last as the ctrl signal
    if(!d->externalSideInput)
        d->sideInput = in >= 0 ? in : -in;

    // 'a' is signal left after subtracting xd (to recompute sideChain envelope)
    double a = d->sideInput - d->xd;
    // a is only needed if positive to pull the envelope up, not to bring it down
    if ( a < 0 ) a=0;
    // the attack/release (peak) exponential filter to guess envelope
    d->xd = d->xd * (1 - d->rt) + d->at * a;

    // if you were to use the rms filter,
    // it would probably look, a sumpthin' like this
    // d->xd = TAV * in*in + (1+TAV) * d->xd

    // decide which slope to use, depending on whether we're below/above thresh
    double slope = d->xd > d->thresh ? d->slopeAbove : d->slopeBelow;
    // the gain function - apply the slope chosen above
    double f = slope == 1.0 ? 1.0 : pow( d->xd / d->thresh, slope - 1.0 );

    // apply the gain found above to input sample
    *out = f * in;

    return TRUE;
}
