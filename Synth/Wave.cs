
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio.Wave;
using NAudio.Wave.SampleProviders;


// https://www.keithmcmillen.com/blog/simple-synthesis-wavetable-synthesis/

// was WvIn.  See >>>> AudioPlaybackPanel
namespace Nebulator.Synth
{
    public class WaveIn// : UGen
    {
        //     WaveIn();

        //     // Overloaded constructor for file input.
        //     WaveIn(string fileName, bool raw = false, bool doNormalize = true, bool generate = true);

        //     // Class destructor.
        //     virtual ~WaveIn();

        //     // Open the specified file and load its data.
        //     virtual void openFile( const string fileName, bool raw = false, bool doNormalize = true, bool generate = true);

        //     // If a file is open, close it.
        //     void closeFile();

        //     // Clear outputs and reset time (file pointer) to zero.
        //     void reset();

        //     // Normalize data to a maximum of +-1.0.
        //       // For large, incrementally loaded files with integer data types,
        //       // normalization is computed relative to the data type maximum.
        //       // No normalization is performed for incrementally loaded files
        //       // with floating-point data types.
        //     void normalize();

        //     // Normalize data to a maximum of +-peak.
        //       // For large, incrementally loaded files with integer data types,
        //       // normalization is computed relative to the data type maximum
        //       // (\e peak/maximum).  For incrementally loaded files with floating-
        //       // point data types, direct scaling by \e peak is performed.
        //     void normalize(double peak);

        //     // Return the file size in sample frames.
        //     ulong getSize();

        //     // Return the number of audio channels in the file.
        //     uint getChannels();

        //     // Return the input file sample rate in Hz (not the data read rate).
        //       // WAV, SND, and AIF formatted files specify a sample rate in
        //       // their headers.  STK RAW files have a sample rate of 22050 Hz
        //       // by definition.  MAT-files are assumed to have a rate of 44100 Hz.
        //     double getFileRate();

        //     // Query whether reading is complete.
        //     bool isFinished();

        //     // Set the data read rate in samples.  The rate can be negative.
        //       // If the rate value is negative, the data is read in reverse order.
        //     void setRate(double aRate);

        //     // Increment the read pointer by \e aTime samples.
        //     virtual void addTime(double aTime);

        //     // Turn linear interpolation on/off.
        //       // Interpolation is automatically off when the read rate is
        //       // an integer value.  If interpolation is turned off for a
        //       // fractional rate, the time index is truncated to an integer
        //       // value.
        //     void setInterpolate(bool doInterpolate);

        //     // Return the average across the last output sample frame.
        //     virtual double lastOut();

        //     // Read out the average across one sample frame of data.
        //     virtual double tick();

        //     // Read out vectorSize averaged sample frames of data in \e vector.
        //     virtual double* tick(double *vector, uint vectorSize);

        //     // Return a pointer to the last output sample frame.
        //     virtual const double* lastFrame() const;

        //     // Return a pointer to the next sample frame of data.
        //     virtual const double *tickFrame();

        //     // Read out sample \e frames of data to \e frameVector.
        //     virtual double *tickFrame(double *frameVector, uint frames);

        // //public: // SWAP formerly protected

        //     // Initialize class variables.
        //     void init();

        //     // Read file data.
        //     virtual void readData(ulong index);

        //     // Get STK RAW file information.
        //     bool getRawInfo( const string fileName );

        //     // Get WAV file header information.
        //     bool getWavInfo( const string fileName );

        //     // Get SND (AU) file header information.
        //     bool getSndInfo( const string fileName );

        //     // Get AIFF file header information.
        //     bool getAifInfo( const string fileName );

        //     // Get MAT-file header information.
        //     bool getMatInfo( const string fileName );

        //     char msg[256];
        //     // char m_filename[256]; // chuck data
        //     Chuck_String str_filename; // chuck data
        //     FILE *fd;
        //     double *data;
        //     double *lastOutput;
        //     bool chunking;
        //     bool finished;
        //     bool interpolate;
        //     bool byteswap;
        //     ulong fileSize;
        //     ulong bufferSize;
        //     ulong dataOffset;
        //     uint channels;
        //     long chunkPointer;
        //     STK_FORMAT dataType;
        //     double fileRate;
        //     double gain;
        //     double time;
        //     double rate;
        //     public:
        //     bool m_loaded;


        private AudioFileReader audioFileReader;

        private ISampleProvider CreateInputStream(string fileName)
        {
            audioFileReader = new AudioFileReader(fileName);

            var sampleChannel = new SampleChannel(audioFileReader, true);
            //sampleChannel.PreVolumeMeter += OnPreVolumeMeter;
            //setVolumeDelegate = vol => sampleChannel.Volume = vol;
            var postVolumeMeter = new MeteringSampleProvider(sampleChannel);
            //postVolumeMeter.StreamVolume += OnPostVolumeMeter;


            string stime = string.Format("{0:00}:{1:00}", (int)audioFileReader.TotalTime.TotalMinutes, audioFileReader.TotalTime.Seconds);

            return postVolumeMeter;
        }

        void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            //groupBoxDriverModel.Enabled = true;
            if (e.Exception != null)
            {
                //MessageBox.Show(e.Exception.Message, "Playback Device Error");
            }

            if (audioFileReader != null)
            {
                audioFileReader.Position = 0;
            }
        }


        //WaveIn()
        //{
        //    init();
        //}

        //WaveIn(string fileName, bool raw, bool doNormalize, bool generate )
        //{
        //    init();
        //    openFile(fileName, raw, generate);
        //}



        void openFile(string fileName, bool raw, bool doNormalize, bool generate)
        {
            //ulong lastChannels = channels;
            //ulong samples, lastSamples = (bufferSize + 1) * channels;
            //str_filename.set(fileName);

            if (!generate || !fileName.StartsWith("special:"))
            {
                //closeFile();

                // Try to open the file.
                //fd = fopen(fileName, "rb");
                //if (!fd)
                //{
                //    sprintf(msg, "[chuck](via WaveIn): Could not open or find file (%s).", fileName);
                //    handleError(msg, StkError::FILE_NOT_FOUND);
                //}

                bool result = false;
                if (raw)
                {
                    result = getRawInfo(fileName);
                }
                //else
                //{
                //    char header[12];
                //    if (fread(&header, 4, 3, fd) != 3) goto error;
                //    if (!strncmp(header, "RIFF", 4) &&
                //            !strncmp(&header[8], "WAVE", 4))
                //        result = getWavInfo(fileName);
                //    else if (!strncmp(header, ".snd", 4))
                //        result = getSndInfo(fileName);
                //    else if (!strncmp(header, "FORM", 4) &&
                //              (!strncmp(&header[8], "AIFF", 4) || !strncmp(&header[8], "AIFC", 4)))
                //        result = getAifInfo(fileName);
                //    else
                //    {
                //        if (fseek(fd, 126, SEEK_SET) == -1) goto error;
                //        if (fread(&header, 2, 1, fd) != 1) goto error;
                //        if (!strncmp(header, "MI", 2) ||
                //                !strncmp(header, "IM", 2))
                //            result = getMatInfo(fileName);
                //        else
                //        {
                //            raw = true;
                //            result = getRawInfo(fileName);
                //            // sprintf(msg, "WaveIn: File (%s) format unknown.", fileName);
                //            // handleError(msg, StkError::FILE_UNKNOWN_FORMAT);
                //        }
                //    }
                //}
            }
            else
            {
                //bufferSize = 1024;
                //channels = 1;
            }


            if (generate && fileName.StartsWith("special:"))
            {
                // STK rawwave files have no header and are assumed to contain a
                // monophonic stream of 16-bit signed integers in big-endian byte
                // order with a sample rate of 22050 Hz.

                //// which
                //if (strstr(fileName, "special:sinewave"))
                //{
                //    for (unsigned int j = 0; j < bufferSize; j++)
                //        data[j] = (SHRT_MAX) * sin(2 * ONE_PI * j / bufferSize);
                //}
                //else
                //{
                //    SAMPLE* rawdata = NULL;
                //    int rawsize = 0;

                //    if (strstr(fileName, "special:aah"))
                //    {
                //        rawsize = ahh_size;
                //        rawdata = ahh_data;
                //    }
                //    else if (strstr(fileName, "special:britestk"))
                //    {
                //        rawsize = britestk_size;
                //        rawdata = britestk_data;
                //    }
                //    else if (strstr(fileName, "special:dope"))
                //    {
                //        rawsize = dope_size;
                //        rawdata = dope_data;
                //    }
                //    else if (strstr(fileName, "special:eee"))
                //    {
                //        rawsize = eee_size;
                //        rawdata = eee_data;
                //    }
                //    else if (strstr(fileName, "special:fwavblnk"))
                //    {
                //        rawsize = fwavblnk_size;
                //        rawdata = fwavblnk_data;
                //    }
                //    else if (strstr(fileName, "special:halfwave"))
                //    {
                //        rawsize = halfwave_size;
                //        rawdata = halfwave_data;
                //    }
                //    else if (strstr(fileName, "special:impuls10"))
                //    {
                //        rawsize = impuls10_size;
                //        rawdata = impuls10_data;
                //    }
                //    else if (strstr(fileName, "special:impuls20"))
                //    {
                //        rawsize = impuls20_size;
                //        rawdata = impuls20_data;
                //    }
                //    else if (strstr(fileName, "special:impuls40"))
                //    {
                //        rawsize = impuls40_size;
                //        rawdata = impuls40_data;
                //    }
                //    else if (strstr(fileName, "special:mand1"))
                //    {
                //        rawsize = mand1_size;
                //        rawdata = mand1_data;
                //    }
                //    else if (strstr(fileName, "special:mandpluk"))
                //    {
                //        rawsize = mandpluk_size;
                //        rawdata = mandpluk_data;
                //    }
                //    else if (strstr(fileName, "special:marmstk1"))
                //    {
                //        rawsize = marmstk1_size;
                //        rawdata = marmstk1_data;
                //    }
                //    else if (strstr(fileName, "special:ooo"))
                //    {
                //        rawsize = ooo_size;
                //        rawdata = ooo_data;
                //    }
                //    else if (strstr(fileName, "special:peksblnk"))
                //    {
                //        rawsize = peksblnk_size;
                //        rawdata = peksblnk_data;
                //    }
                //    else if (strstr(fileName, "special:ppksblnk"))
                //    {
                //        rawsize = ppksblnk_size;
                //        rawdata = ppksblnk_data;
                //    }
                //    else if (strstr(fileName, "special:silence"))
                //    {
                //        rawsize = silence_size;
                //        rawdata = silence_data;
                //    }
                //    else if (strstr(fileName, "special:sineblnk"))
                //    {
                //        rawsize = sineblnk_size;
                //        rawdata = sineblnk_data;
                //    }
                //    else if (strstr(fileName, "special:sinewave"))
                //    {
                //        rawsize = sinewave_size;
                //        rawdata = sinewave_data;
                //    }
                //    else if (strstr(fileName, "special:snglpeak"))
                //    {
                //        rawsize = snglpeak_size;
                //        rawdata = snglpeak_data;
                //    }
                //    else if (strstr(fileName, "special:twopeaks"))
                //    {
                //        rawsize = twopeaks_size;
                //        rawdata = twopeaks_data;
                //    }
                //    else if (strstr(fileName, "special:glot_pop"))
                //    {
                //        rawsize = glot_pop_size;
                //        rawdata = glot_pop_data;
                //        fileRate = 44100.0;
                //        rate = (double)44100.0 / Stk::sampleRate();
                //    }
                //    else if (strstr(fileName, "special:glot_ahh"))
                //    {
                //        rawsize = glot_ahh_size;
                //        rawdata = glot_ahh_data;
                //        fileRate = 44100.0;
                //        rate = (double)44100.0 / Stk::sampleRate();
                //    }
                //    else if (strstr(fileName, "special:glot_eee"))
                //    {
                //        rawsize = glot_eee_size;
                //        rawdata = glot_eee_data;
                //        fileRate = 44100.0;
                //        rate = (double)44100.0 / Stk::sampleRate();
                //    }
                //    else if (strstr(fileName, "special:glot_ooo"))
                //    {
                //        rawsize = glot_ooo_size;
                //        rawdata = glot_ooo_data;
                //        fileRate = 44100.0;
                //        rate = (double)44100.0 / Stk::sampleRate();
                //    }

                //    if (rawdata)
                //    {
                //        if (data) delete[] data;
                //        data = (double*)new double[rawsize + 1];
                //        bufferSize = rawsize;
                //        fileSize = bufferSize;
                //        for (int j = 0; j < rawsize; j++)
                //        {
                //            data[j] = (double)rawdata[j];
                //        }
                //    }
                //    else
                //        goto error;
                //}
                //data[bufferSize] = data[0];
            }
            else
            {
                //readData(0);  // Load file data.
            }

            if (doNormalize)
            {
                ///???                normalize();

            }

            //m_loaded = true;
            //finished = false;
            //interpolate = (fmod(rate, 1.0) != 0.0);
        }

        bool getRawInfo(string fileName)
        {
            // Use the system call "stat" to determine the file length.
            //struct stat filestat;
            //if (stat(fileName, &filestat) == -1 )
            //{
            //    sprintf(msg, "[chuck](via WaveIn): Could not stat RAW file (%s).", fileName);
            //    return false;
            //}

            //fileSize = (long) filestat.st_size / 2;  // length in 2-byte samples
            //bufferSize = fileSize;
            //if (fileSize > CHUNK_THRESHOLD)
            //{
            //    chunking = true;
            //    bufferSize = CHUNK_SIZE;
            //    gain = 1.0 / 32768.0;
            //}

            //// STK rawwave files have no header and are assumed to contain a
            //// monophonic stream of 16-bit signed integers in big-endian byte
            //// order with a sample rate of 22050 Hz.
            //channels = 1;
            //dataOffset = 0;
            //rate = (double) 22050.0 / Stk::sampleRate();
            //fileRate = 22050.0;
            //interpolate = false;
            //dataType = STK_SINT16;
            //byteswap = false;
            //if(little_endian )
            //    byteswap = true;

            return true;
        }

        bool getWavInfo(string fileName)
        {
            //// Find "format" chunk ... it must come before the "data" chunk.
            //char id[4];
            //SINT32 chunkSize;
            //if (fread(&id, 4, 1, fd) != 1) goto error;
            //while (strncmp(id, "fmt ", 4))
            //{
            //    if (fread(&chunkSize, 4, 1, fd) != 1) goto error;
            //    if (!little_endian)
            //        swap32((unsigned char *) & chunkSize);

            //    if (fseek(fd, chunkSize, SEEK_CUR) == -1) goto error;
            //    if (fread(&id, 4, 1, fd) != 1) goto error;
            //}

            //// Check that the data is not compressed.
            //SINT16 format_tag;
            //if (fread(&chunkSize, 4, 1, fd) != 1) goto error; // Read fmt chunk size.
            //if (fread(&format_tag, 2, 1, fd) != 1) goto error;
            //if (!little_endian)
            //{
            //    swap16((unsigned char *) & format_tag);
            //    swap32((unsigned char *) & chunkSize);
            //}
            //if (format_tag != 1 && format_tag != 3)   // PCM = 1, FLOAT = 3
            //{
            //    sprintf(msg, "[chuck](via WaveIn): %s contains an unsupported data format type (%d).", fileName, format_tag);
            //    return false;
            //}

            //// Get number of channels from the header.
            //SINT16 temp;
            //if (fread(&temp, 2, 1, fd) != 1) goto error;
            //if (!little_endian)
            //    swap16((unsigned char *) & temp);

            //channels = (unsigned int ) temp;

            //// Get file sample rate from the header.
            //SINT32 srate;
            //if (fread(&srate, 4, 1, fd) != 1) goto error;
            //if (!little_endian)
            //    swap32((unsigned char *) & srate);

            //fileRate = (double)srate;

            //// Set default rate based on file sampling rate.
            //rate = (double)(srate / Stk::sampleRate());

            //// Determine the data type.
            //dataType = 0;
            //if (fseek(fd, 6, SEEK_CUR) == -1) goto error;   // Locate bits_per_sample info.
            //if (fread(&temp, 2, 1, fd) != 1) goto error;
            //if (!little_endian)
            //    swap16((unsigned char *) & temp);

            //if (format_tag == 1)
            //{
            //    if (temp == 8)
            //        dataType = STK_SINT8;
            //    else if (temp == 16)
            //        dataType = STK_SINT16;
            //    else if (temp == 32)
            //        dataType = STK_SINT32;
            //}
            //else if (format_tag == 3)
            //{
            //    if (temp == 32)
            //        dataType = double32;
            //    else if (temp == 64)
            //        dataType = double64;
            //}
            //if (dataType == 0)
            //{
            //    sprintf(msg, "[chuck](via WaveIn): %d bits per sample with data format %d are not supported (%s).", temp, format_tag, fileName);
            //    return false;
            //}

            //// Jump over any remaining part of the "fmt" chunk.
            //if (fseek(fd, chunkSize - 16, SEEK_CUR) == -1) goto error;

            //// Find "data" chunk ... it must come after the "fmt" chunk.
            //if (fread(&id, 4, 1, fd) != 1) goto error;

            //while (strncmp(id, "data", 4))
            //{
            //    if (fread(&chunkSize, 4, 1, fd) != 1) goto error;
            //    if (!little_endian)
            //        swap32((unsigned char *) & chunkSize);

            //    if (fseek(fd, chunkSize, SEEK_CUR) == -1) goto error;
            //    if (fread(&id, 4, 1, fd) != 1) goto error;
            //}

            //// Get length of data from the header.
            //SINT32 bytes;
            //if (fread(&bytes, 4, 1, fd) != 1) goto error;
            //if (!little_endian)
            //    swap32((unsigned char *) & bytes);

            //fileSize = 8 * bytes / temp / channels;  // sample frames
            //bufferSize = fileSize;
            //if (fileSize > CHUNK_THRESHOLD)
            //{
            //    chunking = true;
            //    bufferSize = CHUNK_SIZE;
            //}

            //dataOffset = ftell(fd);
            //byteswap = false;
            //if (!little_endian)
            //    byteswap = true;

            return true;
        }

        //bool getSndInfo( const char* fileName)
        //{
        //    // Determine the data type.
        //    SINT32 format;
        //    if (fseek(fd, 12, SEEK_SET) == -1) goto error;   // Locate format
        //    if (fread(&format, 4, 1, fd) != 1) goto error;
        //    if (little_endian)
        //        swap32((unsigned char *) & format);

        //    if (format == 2) dataType = STK_SINT8;
        //    else if (format == 3) dataType = STK_SINT16;
        //    else if (format == 5) dataType = STK_SINT32;
        //    else if (format == 6) dataType = double32;
        //    else if (format == 7) dataType = double64;
        //    else
        //    {
        //        sprintf(msg, "[chuck](via WaveIn): data format in file %s is not supported.", fileName);
        //        return false;
        //    }

        //    // Get file sample rate from the header.
        //    SINT32 srate;
        //    if (fread(&srate, 4, 1, fd) != 1) goto error;
        //    if (little_endian)
        //        swap32((unsigned char *) & srate);

        //    fileRate = (double)srate;

        //    // Set default rate based on file sampling rate.
        //    rate = (double)(srate / sampleRate());

        //    // Get number of channels from the header.
        //    SINT32 chans;
        //    if (fread(&chans, 4, 1, fd) != 1) goto error;
        //    if (little_endian)
        //        swap32((unsigned char *) & chans);

        //    channels = chans;

        //    if (fseek(fd, 4, SEEK_SET) == -1) goto error;
        //    if (fread(&dataOffset, 4, 1, fd) != 1) goto error;
        //    if (little_endian)
        //        swap32((unsigned char *) & dataOffset);

        //    // Get length of data from the header.
        //    if (fread(&fileSize, 4, 1, fd) != 1) goto error;
        //    if (little_endian)
        //        swap32((unsigned char *) & fileSize);

        //    fileSize /= 2 * channels;  // Convert to sample frames.
        //    bufferSize = fileSize;
        //    if (fileSize > CHUNK_THRESHOLD)
        //    {
        //        chunking = true;
        //        bufferSize = CHUNK_SIZE;
        //    }

        //    byteswap = false;
        //    if (little_endian)
        //        byteswap = true;

        //    return true;

        //    error:
        //    sprintf(msg, "[chuck](via WaveIn): Error reading SND file (%s).", fileName);
        //    return false;
        //}

        //bool getAifInfo( const char* fileName)
        //{
        //    bool aifc = false;
        //    char id[4];

        //    // Determine whether this is AIFF or AIFC.
        //    if (fseek(fd, 8, SEEK_SET) == -1) goto error;
        //    if (fread(&id, 4, 1, fd) != 1) goto error;
        //    if (!strncmp(id, "AIFC", 4)) aifc = true;

        //    // Find "common" chunk
        //    SINT32 chunkSize;
        //    if (fread(&id, 4, 1, fd) != 1) goto error;
        //    while (strncmp(id, "COMM", 4))
        //    {
        //        if (fread(&chunkSize, 4, 1, fd) != 1) goto error;
        //        if (little_endian)
        //            swap32((unsigned char *) & chunkSize);

        //        if (fseek(fd, chunkSize, SEEK_CUR) == -1) goto error;
        //        if (fread(&id, 4, 1, fd) != 1) goto error;
        //    }

        //    // Get number of channels from the header.
        //    SINT16 temp;
        //    if (fseek(fd, 4, SEEK_CUR) == -1) goto error; // Jump over chunk size
        //    if (fread(&temp, 2, 1, fd) != 1) goto error;
        //    if (little_endian)
        //        swap16((unsigned char *) & temp);

        //    channels = temp;

        //    // Get length of data from the header.
        //    SINT32 frames;
        //    if (fread(&frames, 4, 1, fd) != 1) goto error;
        //    if (little_endian)
        //        swap32((unsigned char *) & frames);

        //    fileSize = frames; // sample frames
        //    bufferSize = fileSize;
        //    if (fileSize > CHUNK_THRESHOLD)
        //    {
        //        chunking = true;
        //        bufferSize = CHUNK_SIZE;
        //    }

        //    // Read the number of bits per sample.
        //    if (fread(&temp, 2, 1, fd) != 1) goto error;
        //    if (little_endian)
        //        swap16((unsigned char *) & temp);

        //    // Get file sample rate from the header.  For AIFF files, this value
        //    // is stored in a 10-byte, IEEE Standard 754 floating point number,
        //    // so we need to convert it first.
        //    unsigned char srate[10];
        //    unsigned char exp;
        //    ulong mantissa;
        //    ulong last;
        //    if (fread(&srate, 10, 1, fd) != 1) goto error;
        //    mantissa = (ulong)*(ulong*)(srate + 2);
        //    if (little_endian)
        //        swap32((unsigned char *) & mantissa);

        //    exp = 30 - *(srate + 1);
        //    last = 0;
        //    while (exp--)
        //    {
        //        last = mantissa;
        //        mantissa >>= 1;
        //    }
        //    if (last & 0x00000001) mantissa++;
        //    fileRate = (double)mantissa;

        //    // Set default rate based on file sampling rate.
        //    rate = (double)(fileRate / sampleRate());

        //    // Determine the data format.
        //    dataType = 0;
        //    if (aifc == false)
        //    {
        //        if (temp == 8) dataType = STK_SINT8;
        //        else if (temp == 16) dataType = STK_SINT16;
        //        else if (temp == 32) dataType = STK_SINT32;
        //    }
        //    else
        //    {
        //        if (fread(&id, 4, 1, fd) != 1) goto error;
        //        if ((!strncmp(id, "fl32", 4) || !strncmp(id, "FL32", 4)) && temp == 32) dataType = double32;
        //        else if ((!strncmp(id, "fl64", 4) || !strncmp(id, "FL64", 4)) && temp == 64) dataType = double64;
        //    }
        //    if (dataType == 0)
        //    {
        //        sprintf(msg, "[chuck](via WaveIn): %d bits per sample in file %s are not supported.", temp, fileName);
        //        return false;
        //    }

        //    // Start at top to find data (SSND) chunk ... chunk order is undefined.
        //    if (fseek(fd, 12, SEEK_SET) == -1) goto error;

        //    // Find data (SSND) chunk
        //    if (fread(&id, 4, 1, fd) != 1) goto error;
        //    while (strncmp(id, "SSND", 4))
        //    {
        //        if (fread(&chunkSize, 4, 1, fd) != 1) goto error;
        //        if (little_endian)
        //            swap32((unsigned char *) & chunkSize);

        //        if (fseek(fd, chunkSize, SEEK_CUR) == -1) goto error;
        //        if (fread(&id, 4, 1, fd) != 1) goto error;
        //    }

        //    // Skip over chunk size, offset, and blocksize fields
        //    if (fseek(fd, 12, SEEK_CUR) == -1) goto error;

        //    dataOffset = ftell(fd);
        //    byteswap = false;
        //    if (little_endian)
        //        byteswap = true;

        //    return true;

        //    error:
        //    sprintf(msg, "[chuck](via WaveIn): Error reading AIFF file (%s).", fileName);
        //    return false;
        //}

        void normalize()
        {
            ///    this->normalize((double)1.0);
        }

        // Normalize all channels equally by the greatest magnitude in all of the data.
        void normalize(double peak)
        {
            //if (chunking)
            //{
            //    if (dataType == STK_SINT8) gain = peak / 128.0;
            //    else if (dataType == STK_SINT16) gain = peak / 32768.0;
            //    else if (dataType == STK_SINT32) gain = peak / 2147483648.0;
            //    else if (dataType == double32 || dataType == double64) gain = peak;

            //    return;
            //}

            //ulong i;
            //double max = (double)0.0;

            //for (i = 0; i < channels * bufferSize; i++)
            //{
            //    if (fabs(data[i]) > max)
            //        max = (double)fabs((double)data[i]);
            //}

            //if (max > 0.0)
            //{
            //    max = (double)1.0 / max;
            //    max *= peak;
            //    for (i = 0; i <= channels * bufferSize; i++)
            //        data[i] *= max;
            //}
        }


        //double tick()
        //{
        //    tickFrame();
        //    return lastOut();
        //}

        //const double* tickFrame()
        //    {
        //        register double tyme, alpha;
        //register ulong i, index;

        //        if (finished) return lastOutput;

        //        tyme = time;
        //        if (chunking)
        //        {
        //            // Check the time address vs. our current buffer limits.
        //            if ((tyme<chunkPointer) || (tyme >= chunkPointer+bufferSize) )
        //                this->readData((long) tyme);
        //            // Adjust index for the current buffer.
        //            tyme -= chunkPointer;
        //        }

        //// Integer part of time address.
        //index = (long) tyme;

        //        if (interpolate)
        //        {
        //            // Linear interpolation ... fractional part of time address.
        //            alpha = tyme - (double) index;
        //index *= channels;
        //            for (i=0; i<channels; i++)
        //            {
        //                lastOutput[i] = data[index];
        //                lastOutput[i] += (alpha* (data[index + channels] - lastOutput[i]));
        //                index++;
        //            }
        //        }
        //        else
        //        {
        //            index *= channels;
        //            for (i=0; i<channels; i++)
        //                lastOutput[i] = data[index++];
        //        }

        //        if (chunking)
        //        {
        //            // Scale outputs by gain.
        //            for (i=0; i<channels; i++)  lastOutput[i] *= gain;
        //        }

        //        // Increment time, which can be negative.
        //        time += rate;
        //        if (time< 0.0 || time >= fileSize ) finished = true;

        //        return lastOutput;
        //    }

        //}

        ///////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////


        // STK waveform oscillator class.

        // This class inherits from WaveIn and provides
        // audio file looping functionality.

        // WaveLoop supports multi-channel data in
        // interleaved format.  It is important to
        // distinguish the tick() methods, which return
        // samples produced by averaging across sample
        // frames, from the tickFrame() methods, which
        // return pointers to multi-channel sample frames.
        // For single-channel data, these methods return
        // equivalent values.

        // by Perry R. Cook and Gary P. Scavone, 1995 - 2002.

        public class WaveLoop : WaveIn
        {
            // WaveLoop();

            // // Class constructor.
            // WaveLoop( const string fileName, bool raw = false, bool generate = true );

            // virtual void openFile( const string  fileName, bool raw = false, bool n = true );

            // // Class destructor.
            // virtual ~WaveLoop();

            // // Set the data interpolation rate based on a looping frequency.
            //   // This function determines the interpolation rate based on the file
            //   // size and the current Stk::sampleRate.  The aFrequency value
            //   // corresponds to file cycles per second.  The frequency can be
            //   // negative, in which case the loop is read in reverse order.
            // void setFrequency(double aFrequency);

            // // Increment the read pointer by aTime samples, modulo file size.
            // void addTime(double aTime);

            // // Increment current read pointer by anAngle, relative to a looping frequency.
            //   // This function increments the read pointer based on the file
            //   // size and the current Stk::sampleRate.  The anAngle value
            //   // is a multiple of file size.
            // void addPhase(double anAngle);

            // // Add a phase offset to the current read pointer.
            //   // This function determines a time offset based on the file
            //   // size and the current Stk::sampleRate.  The \e anAngle value
            //   // is a multiple of file size.
            // void addPhaseOffset(double anAngle);

            // // Return a pointer to the next sample frame of data.
            // const double *tickFrame();

            // public:

            // // Read file data.
            // void readData(ulong index);
            // double phaseOffset;
            // double m_freq; // chuck data;

            /******
                WaveLoop( const char* fileName, bool raw, bool generate )
                    : WaveIn(fileName, raw ), phaseOffset(0.0)
                {
                    m_freq = 0;
                    // If at end of file, redo extra sample frame for looping.
                    if (chunkPointer + bufferSize == fileSize)
                    {
                        for (unsigned int j = 0; j < channels; j++)
                            data[bufferSize * channels + j] = data[j];
                    }

                }

                WaveLoop()
                    : WaveIn(), phaseOffset(0.0)
                {
                    m_freq = 0;
                }

                void openFile( const char* fileName, bool raw, bool norm )
                {
                    m_loaded = false;
                    WaveIn::openFile(fileName, raw, norm);
                    // If at end of file, redo extra sample frame for looping.
                    if (chunkPointer+bufferSize == fileSize)
                    {
                        for (unsigned int j = 0; j<channels; j++)
                            data[bufferSize * channels + j] = data[j];
                    }
            m_loaded = true;
                }

                void readData(ulong index)
            {
                WaveIn::readData(index);

                // If at end of file, redo extra sample frame for looping.
                if (chunkPointer + bufferSize == fileSize)
                {
                    for (unsigned int j = 0; j < channels; j++)
                        data[bufferSize * channels + j] = data[j];
                }
            }

            void setFrequency(double aFrequency)
            {
                // This is a looping frequency.
                m_freq = aFrequency; // chuck data

                rate = fileSize * aFrequency / sampleRate();
            }

            void WaveLoop :: addTime(double aTime)
            {
                // Add an absolute time in samples
                time += aTime;

                while (time < 0.0)
                    time += fileSize;
                while (time >= fileSize)
                    time -= fileSize;
            }

            void addPhase(double anAngle)
            {
                // Add a time in cycles (one cycle = fileSize).
                time += fileSize * anAngle;

                while (time < 0.0)
                    time += fileSize;
                while (time >= fileSize)
                    time -= fileSize;
            }

            void addPhaseOffset(double anAngle)
            {
                // Add a phase offset in cycles, where 1.0 = fileSize.
                phaseOffset = fileSize * anAngle;
            }

            const double* tickFrame()
                {
                    register double tyme, alpha;
            register ulong i, index;

                    // Check limits of time address ... if necessary, recalculate modulo fileSize.
                    while (time< 0.0)
                        time += fileSize;
                    while (time >= fileSize)
                        time -= fileSize;

                    if (phaseOffset)
                    {
                        tyme = time + phaseOffset;
                        while (tyme< 0.0)
                            tyme += fileSize;
                        while (tyme >= fileSize)
                            tyme -= fileSize;
                    }
                    else
                    {
                        tyme = time;
                    }

                    if (chunking)
                    {
                        // Check the time address vs. our current buffer limits.
                        if ((tyme<chunkPointer) || (tyme >= chunkPointer+bufferSize) )
                            this->readData((long) tyme);
                        // Adjust index for the current buffer.
                        tyme -= chunkPointer;
                    }

            // Always do linear interpolation here ... integer part of time address.
            index = (ulong) tyme;

            // Fractional part of time address.
            alpha = tyme - (double) index;
            index *= channels;
                    for (i=0; i<channels; i++)
                    {
                        lastOutput[i] = data[index];
                        lastOutput[i] += (alpha* (data[index + channels] - lastOutput[i]));
                        index++;
                    }

                    if (chunking)
                    {
                        // Scale outputs by gain.
                        for (i=0; i<channels; i++)  lastOutput[i] *= gain;
                    }

                    // Increment time, which can be negative.
                    time += rate;

                    return lastOutput;
                }

            }
            ***/
        }
    }
}