using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace BangBits.ER301.Driver
{
    enum Response : byte
    {
        OK = 0,
        ERR_BAUD_RATE = 1,
        ERR_PORT_DISCONNECT = 2,
        ERR_GENERAL = 10,
        ERR_UNDEFINED = 11,
        ERR_COMMAND_PARAMETER = 12,
        ERR_NO_CARD = 13,
        ERR_REQUEST_FAILURE = 20, // 0x14, no card present
        ERR_RESET_FAILURE = 21,
        ERR_AUTH_FAILURE = 22,
        ERR_READ_BLOCK_FAILURE = 23,
        ERR_WRITE_BLOCK_FAILURE = 24,
        ERR_WRITE_ADDRESS_FAILURE = 25,
        ERR_WRITE_ADDRESS_FAILURE2 = 26
    };

    enum Command : ushort
    {
        INIT_PORT = 0x0101,
        SET_DEVICE_NODE_NO = 0x0102,
        GET_DEVICE_NODE_NO = 0x0103,
        READ_DEVICE_MODE = 0x0104,
        SET_BUZZER_BEEP = 0x0106,
        SET_LED_COLOR = 0x0107,
        RFU = 0x0108,
        SET_ANTENNA_STATUS = 0x010c,
        MIFARE_REQUEST = 0x0201,
        MIFARE_ANTICOLLISION = 0x0202,
        MIFARE_SELECT = 0x0203,
        MIFARE_HLTA = 0x0204,
        MIFARE_AUTHENTICATION2 = 0x0207,
        MIFARE_READ = 0x0208,
        MIFARE_WRITE = 0x0209,
        MIFARE_INITVAL = 0x020A,
        MIFARE_READBALANCE = 0x020B,
        MIFARE_DECREMENT = 0x020C,
        MIFARE_INCREMENT = 0x020D
    };

    enum LEDColor : byte
    {
        ALL_LED_OFF = 0x00,
        BLUE_ON_RED_OFF = 0x01,
        RED_ON_BLUE_OFF = 0x02
    };

    enum BeepType : byte
    {
        SHORT_60MS = 6,
        MEDIUM_200MS = 20,
        LONG_600MS = 60
    };

    enum AntennaStatus : byte
    {
        CLOSED = 0x00,
        OPEN = 0x01
    };

    enum MifareRequestCode : byte
    {
        IDLE_CARD = 0x26,	// If the card is halted, it won't respond
        ALL_TYPE_A = 0x52  // Will activate even a halted card
    };

    public enum AuthMode : byte
    {
        KEY_A = 0x60,
        KEY_B = 0x61
    };

    enum MifareType : ushort
    {
        ULTRALIGHT = 0x0044,
        MIFARE_CLASSIC_1K_S50 = 0x0004,

        // The Mifare Classic 4k has 4096 bytes across 40 sectors: Sector 0-31 are divided into 4 discrete 16-byte 
        // blocks (2Kb). Sector 32-39  are divided into 16 discrete 16-byte blocks (2Kb). Block 0 is accessable 
        // without authorization, but all other remaining blocks require authorization per sector basis. Last block 
        // in a sector (sector trailer) is reserved for the storage of the auth info. This means there are 41 
        // non-writable blocks (656 bytes), leaving 215 writable block (3440 bytes). The auth blocks are divided 
        // into 6 bytes for key A, 4 bytes of read/write configuration and 6 bytes for key B.
        // Accessing a block is a two step process. First you must authenticate to the sector with either the 
        // A or the B key, then you can read or write one of the blocks in that sector.
        //
        MIFARE_CLASSIC_4K_S70 = 0x0002,
        DESFIRE = 0x0344,
        PRO = 0x0008,
        PRO_X = 0x0304
    };

    enum UartSpeed : byte
    {
        BAUD_4800,
        BAUD_9600,
        BAUD_14400,
        BAUD_19200,
        BAUD_28800,
        BAUD_38400,
        BAUD_57600,
        BAUD_115200
    };

    class MifareResponse
    {
        private Response response;
        private ushort nodeId;
        private Command command;

        public Response Response
        {
            get
            {
                return response;
            }
            set
            {
                response = value;
            }
        }

        public ushort NodeId
        {
            get
            {
                return nodeId;
            }
            set
            {
                nodeId = value;
            }
        }

        public Command Command
        {
            get
            {
                return command;
            }
            set
            {
                command = value;
            }
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("{Command=");
            stringBuilder.Append(this.Command);

            stringBuilder.Append(", NodeId=");
            stringBuilder.Append(this.NodeId);

            stringBuilder.Append(", ResponseCode=");
            stringBuilder.Append(this.Response);

            stringBuilder.Append("}");

            return stringBuilder.ToString();
        }
    }

    class MifareDeviceResponse : MifareResponse
    {
        private string deviceName;

        public string DeviceName
        {
            get
            {
                return this.deviceName;
            }
            set
            {
                deviceName = value;
            }
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("{Command=");
            stringBuilder.Append(this.Command);

            stringBuilder.Append(", NodeId=");
            stringBuilder.Append(this.NodeId);

            stringBuilder.Append(", ResponseCode=");
            stringBuilder.Append(this.Response);

            stringBuilder.Append(", DeviceName=");
            stringBuilder.Append(this.DeviceName);

            stringBuilder.Append("}");

            return stringBuilder.ToString();
        }

    }

    class MifareTypeResponse : MifareResponse
    {
        private MifareType mifareType;

        public MifareType MifareType
        {
            get
            {
                return this.mifareType;
            }
            set
            {
                mifareType = value;
            }
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("{Command=");
            stringBuilder.Append(this.Command);

            stringBuilder.Append(", NodeId=");
            stringBuilder.Append(this.NodeId);

            stringBuilder.Append(", ResponseCode=");
            stringBuilder.Append(this.Response);

            stringBuilder.Append(", MifareType=");
            stringBuilder.Append(this.MifareType);

            stringBuilder.Append("}");

            return stringBuilder.ToString();
        }
    }

    class AnticollisionResponse : MifareResponse
    {
        private uint serialNo;

        public uint SerialNo
        {
            get
            {
                return this.serialNo;
            }
            set
            {
                serialNo = value;
            }
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("{Command=");
            stringBuilder.Append(this.Command);

            stringBuilder.Append(", NodeId=");
            stringBuilder.Append(this.NodeId);

            stringBuilder.Append(", ResponseCode=");
            stringBuilder.Append(this.Response);

            stringBuilder.Append(", SerialNo=");
            stringBuilder.Append(this.serialNo);

            stringBuilder.Append("}");

            return stringBuilder.ToString();
        }
    }

    class SelectResponse : MifareResponse
    {
        // WARNING: There's something fishy about this SAK byte. According to
        // specifications it ought to hold the Select AcKnowledge byte which
        // helps to identy the card. However, with the ER301 reader, this byte
        // is always 9!?
        private byte sakByte;

        public byte SAKByte
        {
            get
            {
                return this.sakByte;
            }
            set
            {
                sakByte = value;
            }
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("{Command=");
            stringBuilder.Append(this.Command);

            stringBuilder.Append(", NodeId=");
            stringBuilder.Append(this.NodeId);

            stringBuilder.Append(", ResponseCode=");
            stringBuilder.Append(this.Response);

            stringBuilder.Append(", SAK=");
            stringBuilder.Append(this.sakByte);

            stringBuilder.Append("}");

            return stringBuilder.ToString();
        }
    }

    class ReadResponse : MifareResponse
    {
        private byte[] blockData;

        public byte[] Data
        {
            get
            {
                return this.blockData;
            }
            set
            {
                blockData = value;
            }
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("{Command=");
            stringBuilder.Append(this.Command);

            stringBuilder.Append(", NodeId=");
            stringBuilder.Append(this.NodeId);

            stringBuilder.Append(", ResponseCode=");
            stringBuilder.Append(this.Response);

            stringBuilder.Append(", Data=");
            stringBuilder.Append(blockData.ToHex());

            stringBuilder.Append("}");

            return stringBuilder.ToString();
        }
    }

    public class Mifare : IDisposable
    {
        public const int MifareClassicS70_BlockCount = 256;
        public const int MifareClassicS70__SectorCount = 40;
        public const int MifareClassicS70_BlockSize = 16;

        const int INIT_REQUEST_RETRY_COUNT = 5;
        const int RETRY_COUNT = 3;

        const ushort MAGIC_BYTES = 0xbbaa;
        const ushort NODE_BROADCAST = 0x0000;

        MifareReadCallback callback;

        CancellationToken cancellationToken = new CancellationTokenSource().Token;

        byte[] buffer = new byte[4096];
        
        SerialPort port;

        public Mifare(string portName, CancellationToken cancellationToken, MifareReadCallback callback) : this(portName, callback)
        {
            this.cancellationToken = cancellationToken;
        }

        public Mifare(string portName, MifareReadCallback callback)
        {
            this.port = new SerialPort();           
            this.callback = callback;

            try
            {
                if (openPort(portName))
                {
                    callback.status(string.Format("Successfully openend port {0}", portName));

                    if (initPort(UartSpeed.BAUD_115200))
                    {
                        callback.status(string.Format("Succesfull initialization of hardware"));
                        startCardDetectionLoop(callback);
                    }
                    else
                    {
                        callback.status(string.Format("Error during initialization of hardware"));
                        callback.error();
                        if (port.IsOpen)
                        {
                            port.Close();
                        }
                    }
                }
                else
                {
                    callback.status(string.Format("Error while trying to open port {0}", portName));
                    callback.error();
                }

            }
            catch (Exception exception)
            {
                callback.status("Connection interupted due to checksum error.");
                callback.error();
            }
        }



        byte[] getRejsekortKeyByBlock(AuthMode authMode, int blockIndex)
        {
            int sectorId = getSectorIndexByBlockIndex(blockIndex);

            if (authMode == AuthMode.KEY_A)
            {
                return callback.getKeyABySector(sectorId);
            }
            else
            {
                return callback.getKeyBBySector(sectorId);
            }
        }

        public static int getSectorIndexByBlockIndex(int blockIndex)
        {
            return (blockIndex < 128) ? (int)blockIndex / 4 : 32 + ((int)(blockIndex - 128) / 16);
        }

        public void Dispose()
        {
            if (port.IsOpen)
            {
                port.Close();
            }
        }

        private bool openPort(string portName)
        {
            port.PortName = portName;
            port.BaudRate = 115200;
            port.DataBits = 8;
            port.StopBits = StopBits.One;
            port.Parity = Parity.None;
            port.RtsEnable = true;
            port.DtrEnable = true;
            port.WriteTimeout = 50;
            port.ReadTimeout = 50;

            try
            {
                port.Open();
            }
            catch (System.UnauthorizedAccessException ex)
            {
                return false;
            }

            // Give the system a little time to open port
            Thread.Sleep(50);

            return port.IsOpen;
        }

        private bool initPort(UartSpeed speed)
        {
            try
            {
                // Let's initialize the port with highest possible bandwidth
                MifareResponse request = sendInitRequest(speed);
                if (request.Response != Response.OK)
                {
                    return false;
                }

                // Let's find out what hardware we're dealing with
                MifareDeviceResponse deviceModeResponse = sendReadDeviceMode();
                if (deviceModeResponse.Response != Response.OK)
                {
                    return false;
                }

                Console.WriteLine("Communicating with device name {0}", deviceModeResponse.DeviceName);

                // Turn LED off so we can actively use it to give visual feedback
                //Console.WriteLine (sendLedRequest (LEDColor.ALL_LED_OFF));
                //Console.WriteLine ();

                sendLedRequest(LEDColor.BLUE_ON_RED_OFF);
                Thread.Sleep(500);
                sendLedRequest(LEDColor.ALL_LED_OFF);

                return true;
            }
            catch (System.TimeoutException exception)
            {
                return false;
            }
        }

        private void startCardDetectionLoop(MifareReadCallback callback)
        {
            // Loop indefinately
            //for (;;)
            {
                try
                {
                    callback.status("Connected to ER301 hardware, waiting for card...");

                    // Wait for Ok detection of card
                    MifareTypeResponse requestResponse = null;
                    do
                    {
                        //requestResponse = sendMifareRequest (MifareRequestCode.IDLE_CARD);
                        requestResponse = sendMifareRequest(MifareRequestCode.ALL_TYPE_A);
                        Thread.Sleep(50);

                        if (cancellationToken.IsCancellationRequested)
                        {
                            // Clean up here, then...
                            //cancellationToken.ThrowIfCancellationRequested();
                            callback.status("Reading of card interupted by user!");
                            callback.error();
                            return;
                        }
                    }
                    while (requestResponse.Response != Response.OK);

                    StartConversation(requestResponse, callback);

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    callback.status("Connection interupted!");
                    callback.error();

                    try
                    {
                        sendLedRequest(LEDColor.RED_ON_BLUE_OFF);

                        sendBeepRequest(BeepType.LONG_600MS);

                        Thread.Sleep(300);

                        sendLedRequest(LEDColor.ALL_LED_OFF);
                    }
                    catch (Exception e2)
                    {
                        // Ignore whatever happens when exercising LED's
                    }

                }

            }
        }

        private void StartConversation(MifareTypeResponse requestResponse, MifareReadCallback callback)
        {
            sendLedRequest(LEDColor.BLUE_ON_RED_OFF);

            // Start anticollistion (and get serialNo)
            AnticollisionResponse collisionResponse = null;
            //for(int retryCount = 0; retryCount < RETRY_COUNT; retryCount++)
            {
                collisionResponse = sendMifareAnticollisionRequest();

                if (collisionResponse.Response == Response.OK)
                {
                    uint serialNo = ReverseBytes(collisionResponse.SerialNo);

                    Console.WriteLine("Card detected, type={0}, serialNo={1}",
                                       requestResponse.MifareType,
                                       serialNo);

                    SelectResponse selectResponse = sendMifareSelect(collisionResponse.SerialNo);

                    if (selectResponse.Response != Response.OK)
                    {
                        throw new Exception("Mifare select error!");
                    }
                    Console.WriteLine("Card {0} selected and active", serialNo);

                    //callback.status(BitConverter.GetBytes(collisionResponse.SerialNo).ToHex());

                    DateTime before = DateTime.Now;

                    Console.WriteLine();

                    callback.status(String.Format("Reading card {0} with serialno. {1}",
                        requestResponse.MifareType,
                        serialNo));
                    
                    int lastAuthorizedSectorIndex = -1;

                    for (int blockIndex = 0; blockIndex < getBlockCount(requestResponse.MifareType); blockIndex++)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            // Clean up here, then...
                            //cancellationToken.ThrowIfCancellationRequested();
                            callback.status("Reading of card interupted by user!");
                            callback.error();
                            return;
                        }

                        int sectorIndex = getSectorIndexByBlockIndex(blockIndex);

                        // If we haven't authorized for this sector before
                        if (sectorIndex != lastAuthorizedSectorIndex)
                        {
                            lastAuthorizedSectorIndex = sectorIndex;

                            //Console.WriteLine("Autorizing sector {0} for block starting at index {1}", sectorIndex, blockIndex);

                            if (callback.getAuthByKeyA())
                            {
                                MifareResponse auth = sendAuth((byte)blockIndex, AuthMode.KEY_A, getRejsekortKeyByBlock(AuthMode.KEY_A, blockIndex));

                                if (auth.Response == Response.ERR_AUTH_FAILURE)
                                {
                                    var response = sendMifareRequest(MifareRequestCode.IDLE_CARD).Response;

                                    callback.status(string.Format("Error while trying to authorize sector {0} with A-key {1}",
                                        getSectorIndexByBlockIndex(blockIndex),
                                        getRejsekortKeyByBlock(AuthMode.KEY_A, blockIndex).ToHex()));
                                    callback.error();
                                    return;
                                }
                            }

                            if (callback.getAuthByKeyB())
                            {
                                MifareResponse auth = sendAuth((byte)blockIndex, AuthMode.KEY_B, getRejsekortKeyByBlock(AuthMode.KEY_B, blockIndex));

                                if (auth.Response == Response.ERR_AUTH_FAILURE)
                                {
                                    var response = sendMifareRequest(MifareRequestCode.IDLE_CARD).Response;

                                    callback.status(string.Format("Error while trying to authorize sector {0} with B-key {1}",
                                        getSectorIndexByBlockIndex(blockIndex),
                                        getRejsekortKeyByBlock(AuthMode.KEY_A, blockIndex).ToHex()));
                                    callback.error();
                                    return;
                                }
                            }

                        }

                        {
                            ReadResponse read = sendRead((byte)blockIndex);

                            // If this block is a sector trailer
                            if (getSectorIndexByBlockIndex(blockIndex + 1) != sectorIndex)
                            {
                                // If keys are supposed to be included in data
                                if (callback.getInclKeys())
                                {
                                    // If authorizing with key A is enabled
                                    if (callback.getAuthByKeyA())
                                    {
                                        Buffer.BlockCopy(callback.getKeyABySector(sectorIndex), 0, read.Data, 0, 6);
                                    }

                                    // If authorizing with key B is enabled
                                    if (callback.getAuthByKeyB())
                                    {
                                        Buffer.BlockCopy(callback.getKeyBBySector(sectorIndex), 0, read.Data, 10, 6);
                                    }
                                }
                            }

                            callback.completeBlock(blockIndex, read.Data);

                            //Console.WriteLine("Copying {0} bytes over to index {1}", 16, (blockIndex*16));

                            Buffer.BlockCopy(read.Data, 0, buffer, blockIndex * 16, 16);
                        }
                    }

                    DateTime after = DateTime.Now;

                    TimeSpan span = after - before;

                    //Console.WriteLine ("Auth took {0} ms", span.TotalMilliseconds);



                    // Done with card, halt session
                    if (sendHalt().Response == Response.OK)
                    {
                        // Turn BLUE LED on to signal All OK
                        //sendLedRequest (LEDColor.BLUE_ON_RED_OFF);

                        sendBeepRequest(BeepType.SHORT_60MS);

                        //Thread.Sleep (500);
                        // Revert to normal LED state off
                        sendLedRequest(LEDColor.ALL_LED_OFF);

                        callback.status(String.Format("Card read in {0},{1} sec.", span.Seconds, span.Milliseconds));
                    }
                    else
                    {
                        //sendLedRequest(LEDColor.RED_ON_BLUE_OFF);

                    }

                    MD5 md5 = System.Security.Cryptography.MD5.Create();
                    string hash = md5.ComputeHash(buffer).ToHex();

                    callback.success(serialNo, hash);

                    return;

                }
            };

        }

        public static UInt32 ReverseBytes(UInt32 value)
        {
            return (value & 0x000000FFU) << 24 | (value & 0x0000FF00U) << 8 |
                   (value & 0x00FF0000U) >> 8 | (value & 0xFF000000U) >> 24;
        }

        private MifareResponse sendInitRequest(UartSpeed speed)
        {
            byte[] readBuffer = new byte[64];
            int offset = 0;
            int bytesToRead = 0;

            int retryCount = 0;
            do
            {
                if (retryCount++ == INIT_REQUEST_RETRY_COUNT)
                {
                    throw new Exception("Failed at establishting connection!");
                }

                WriteCommand(Command.INIT_PORT, NODE_BROADCAST, (byte)speed);

                Thread.Sleep(50);

                bytesToRead = port.BytesToRead;

            } while (bytesToRead == 0);

            Console.WriteLine("Connection established after " + retryCount + " attempts!");

            // TODO: Use callback, make response parser able to read lazily from input stream
            int bytesRead = port.Read(readBuffer, offset, bytesToRead);


            // Evaluate data in buffer


            //Console.WriteLine ();
            //Console.WriteLine ("Received: {0} ( {1} bytes)", readBuffer.Subset (0, offset).ToHex (), offset);

            MifareResponse response = new MifareResponse();

            //ushort magic = readBuffer.Subset (0, 2).ToUInt16 ();
            //Console.WriteLine ("Magic: {0} ( {1})", magic, readBuffer.Subset (0, 2).ToHex ());

            ushort length = readBuffer.Subset(2, 2).ToUInt16();
            //Console.WriteLine ("Length: {0} ( {1})", length, readBuffer.Subset (2, 2).ToHex ());

            ushort nodeID = readBuffer.Subset(4, 2).ToUInt16();
            //Console.WriteLine ("NodeID: {0} ( {1})", nodeID, readBuffer.Subset (4, 2).ToHex ());
            response.NodeId = nodeID;

            ushort commandCode = readBuffer.Subset(6, 2).ToUInt16();
            //Console.WriteLine ("Command: {0} ( {1})", Enum.GetName (typeof(CommandCode), commandCode), readBuffer.Subset (6, 2).ToHex ());
            response.Command = (Command)commandCode;

            byte responseCode = readBuffer[8];
            //Console.WriteLine ("Response Code: {0} ( {1})", Enum.GetName (typeof(ResponseCode), responseCode), responseCode.ToHex ());
            response.Response = (Response)responseCode;

            //byte checksum = readBuffer [readBuffer.Length-1];
            byte checksum = readBuffer[4 + length];
            //Console.WriteLine ("XOR: {0} ( {1})", checksum, checksum.ToHex ());

            byte calculatedChecksum = CalcCheckSum(readBuffer, 4, 3 + length);
            //Console.WriteLine ("XOR match: {0} ( {1})", calculatedChecksum, calculatedChecksum.ToHex ());

            validateChecksum(checksum, calculatedChecksum);

            return response;
        }

        private MifareDeviceResponse sendReadDeviceMode()
        {
            byte[] readBuffer = new byte[64];
            int offset = 0;
            int bytesToRead = 0;

            WriteCommand(Command.READ_DEVICE_MODE, NODE_BROADCAST);

            // Naive busy way. Rewrite to consumer stream pull!
            do
            {
                Thread.Sleep(50);
                bytesToRead = port.BytesToRead;
            } while (bytesToRead == 0);

            // TODO: Use callback, make response parser able to read lazily from input stream
            int bytesRead = port.Read(readBuffer, offset, bytesToRead);

            // Evaluate data in buffer

            MifareDeviceResponse response = new MifareDeviceResponse();

            ushort length = readBuffer.Subset(2, 2).ToUInt16();
            //Console.WriteLine ("Length: {0} ( {1})", length, readBuffer.Subset (2, 2).ToHex ());

            ushort nodeID = readBuffer.Subset(4, 2).ToUInt16();
            //Console.WriteLine ("NodeID: {0} ( {1})", nodeID, readBuffer.Subset (4, 2).ToHex ());
            response.NodeId = nodeID;

            ushort commandCode = readBuffer.Subset(6, 2).ToUInt16();
            //Console.WriteLine ("Command: {0} ( {1})", Enum.GetName (typeof(CommandCode), commandCode), readBuffer.Subset (6, 2).ToHex ());
            response.Command = (Command)commandCode;

            byte responseCode = readBuffer[8];

            //Console.WriteLine ("Response Code: {0} ( {1})", Enum.GetName (typeof(Response), responseCode), responseCode.ToHex ());
            response.Response = (Response)responseCode;

            byte[] name = readBuffer.Subset(10, 7 + length);
            response.DeviceName = System.Text.Encoding.ASCII.GetString(name);

            //byte checksum = readBuffer [readBuffer.Length-1];
            byte checksum = readBuffer[4 + length];

            //Console.WriteLine ("XOR: {0} ( {1})", checksum, checksum.ToHex ());

            byte calculatedChecksum = CalcCheckSum(readBuffer, 4, 4 + length);
            //Console.WriteLine ("XOR match: {0} ( {1})", calculatedChecksum, calculatedChecksum.ToHex ());

            validateChecksum(checksum, calculatedChecksum);

            return response;
        }


        private MifareTypeResponse sendMifareRequest(MifareRequestCode requestCode)
        {
            byte[] readBuffer = new byte[64];
            int offset = 0;
            int bytesToRead = 0;

            WriteCommand(Command.MIFARE_REQUEST, NODE_BROADCAST, (byte)requestCode);

            // Naive busy way. Rewrite to consumer stream pull!
            do
            {
                Thread.Sleep(10);
                bytesToRead = port.BytesToRead;
            } while (bytesToRead == 0);

            // TODO: Use callback, make response parser able to read lazily from input stream
            int bytesRead = port.Read(readBuffer, offset, bytesToRead);

            // Evaluate data in buffer

            MifareTypeResponse response = new MifareTypeResponse();

            ushort length = readBuffer.Subset(2, 2).ToUInt16();
            //Console.WriteLine ("Length: {0} ( {1})", length, readBuffer.Subset (2, 2).ToHex ());

            ushort nodeID = readBuffer.Subset(4, 2).ToUInt16();
            //Console.WriteLine ("NodeID: {0} ( {1})", nodeID, readBuffer.Subset (4, 2).ToHex ());
            response.NodeId = nodeID;

            ushort commandCode = readBuffer.Subset(6, 2).ToUInt16();
            //Console.WriteLine ("Command: {0} ( {1})", Enum.GetName (typeof(CommandCode), commandCode), readBuffer.Subset (6, 2).ToHex ());
            response.Command = (Command)commandCode;

            byte responseCode = readBuffer[8];

            //Console.WriteLine ("Response Code: {0} ( {1})", Enum.GetName (typeof(ResponseCode), responseCode), responseCode.ToHex ());
            response.Response = (Response)responseCode;

            byte[] type = readBuffer.Subset(9, 2);
            //Console.WriteLine ("Type: " + type.ToHex());
            response.MifareType = (MifareType)BitConverter.ToUInt16(type, 0);

            //byte checksum = readBuffer [readBuffer.Length-1];
            byte checksum = readBuffer[4 + length];

            //Console.WriteLine ("XOR: {0} ( {1})", checksum, checksum.ToHex ());

            byte calculatedChecksum = CalcCheckSum(readBuffer, 4, 4 + length);
            //Console.WriteLine ("XOR match: {0} ( {1})", calculatedChecksum, calculatedChecksum.ToHex ());

            validateChecksum(checksum, calculatedChecksum);

            return response;
        }


        private AnticollisionResponse sendMifareAnticollisionRequest()
        {
            byte[] readBuffer = new byte[64];
            int offset = 0;
            int bytesToRead = 0;

            WriteCommand(Command.MIFARE_ANTICOLLISION, NODE_BROADCAST);

            // Naive busy way. Rewrite to consumer stream pull!
            do
            {
                Thread.Sleep(50);
                bytesToRead = port.BytesToRead;
            } while (bytesToRead == 0);

            // TODO: Use callback, make response parser able to read lazily from input stream
            int bytesRead = port.Read(readBuffer, offset, bytesToRead);

            // Evaluate data in buffer

            AnticollisionResponse response = new AnticollisionResponse();

            ushort length = readBuffer.Subset(2, 2).ToUInt16();
            //Console.WriteLine ("Length: {0} ( {1})", length, readBuffer.Subset (2, 2).ToHex ());

            ushort nodeID = readBuffer.Subset(4, 2).ToUInt16();
            //Console.WriteLine ("NodeID: {0} ( {1})", nodeID, readBuffer.Subset (4, 2).ToHex ());
            response.NodeId = nodeID;

            ushort commandCode = readBuffer.Subset(6, 2).ToUInt16();
            //Console.WriteLine ("Command: {0} ( {1})", Enum.GetName (typeof(CommandCode), commandCode), readBuffer.Subset (6, 2).ToHex ());
            response.Command = (Command)commandCode;

            byte responseCode = readBuffer[8];

            //Console.WriteLine ("Response Code: {0} ( {1})", Enum.GetName (typeof(ResponseCode), responseCode), responseCode.ToHex ());
            response.Response = (Response)responseCode;

            byte[] serial = readBuffer.Subset(9, 4);
            //Console.WriteLine ("Serial: " + serial.ToHex());
            response.SerialNo = BitConverter.ToUInt32(serial, 0);

            //byte checksum = readBuffer [readBuffer.Length-1];
            byte checksum = readBuffer[4 + length];

            //Console.WriteLine ("XOR: {0} ( {1})", checksum, checksum.ToHex ());

            byte calculatedChecksum = CalcCheckSum(readBuffer, 4, 4 + length);
            //Console.WriteLine ("XOR match: {0} ( {1})", calculatedChecksum, calculatedChecksum.ToHex ());

            validateChecksum(checksum, calculatedChecksum);

            return response;
        }

        private MifareResponse sendLedRequest(LEDColor ledColor)
        {
            byte[] readBuffer = new byte[64];
            int offset = 0;
            int bytesToRead = 0;

            WriteCommand(Command.SET_LED_COLOR, NODE_BROADCAST, (byte)ledColor);

            // Naive busy way. Rewrite to consumer stream pull!
            do
            {
                Thread.Sleep(50);
                bytesToRead = port.BytesToRead;
            } while (bytesToRead == 0);

            // TODO: Use callback, make response parser able to read lazily from input stream
            int bytesRead = port.Read(readBuffer, offset, bytesToRead);

            // Evaluate data in buffer

            MifareResponse response = new MifareResponse();

            ushort length = readBuffer.Subset(2, 2).ToUInt16();
            //Console.WriteLine ("Length: {0} ( {1})", length, readBuffer.Subset (2, 2).ToHex ());

            ushort nodeID = readBuffer.Subset(4, 2).ToUInt16();
            //Console.WriteLine ("NodeID: {0} ( {1})", nodeID, readBuffer.Subset (4, 2).ToHex ());
            response.NodeId = nodeID;

            ushort commandCode = readBuffer.Subset(6, 2).ToUInt16();
            //Console.WriteLine ("Command: {0} ( {1})", Enum.GetName (typeof(CommandCode), commandCode), readBuffer.Subset (6, 2).ToHex ());
            response.Command = (Command)commandCode;

            byte responseCode = readBuffer[8];

            //Console.WriteLine ("Response Code: {0} ( {1})", Enum.GetName (typeof(ResponseCode), responseCode), responseCode.ToHex ());
            response.Response = (Response)responseCode;

            //byte checksum = readBuffer [readBuffer.Length-1];
            byte checksum = readBuffer[4 + length];

            byte calculatedChecksum = CalcCheckSum(readBuffer, 4, 4 + length);

            validateChecksum(checksum, calculatedChecksum);

            return response;
        }

        private MifareResponse sendBeepRequest(BeepType beepType)
        {
            byte[] readBuffer = new byte[64];
            int offset = 0;
            int bytesToRead = 0;

            WriteCommand(Command.SET_BUZZER_BEEP, NODE_BROADCAST, (byte)beepType);

            // Naive busy way. Rewrite to consumer stream pull!
            do
            {
                Thread.Sleep(50);
                bytesToRead = port.BytesToRead;
            } while (bytesToRead == 0);

            // TODO: Use callback, make response parser able to read lazily from input stream
            int bytesRead = port.Read(readBuffer, offset, bytesToRead);

            // Evaluate data in buffer

            MifareResponse response = new MifareResponse();

            ushort length = readBuffer.Subset(2, 2).ToUInt16();
            //Console.WriteLine ("Length: {0} ( {1})", length, readBuffer.Subset (2, 2).ToHex ());

            ushort nodeID = readBuffer.Subset(4, 2).ToUInt16();
            //Console.WriteLine ("NodeID: {0} ( {1})", nodeID, readBuffer.Subset (4, 2).ToHex ());
            response.NodeId = nodeID;

            ushort commandCode = readBuffer.Subset(6, 2).ToUInt16();
            //Console.WriteLine ("Command: {0} ( {1})", Enum.GetName (typeof(CommandCode), commandCode), readBuffer.Subset (6, 2).ToHex ());
            response.Command = (Command)commandCode;

            byte responseCode = readBuffer[8];

            //Console.WriteLine ("Response Code: {0} ( {1})", Enum.GetName (typeof(ResponseCode), responseCode), responseCode.ToHex ());
            response.Response = (Response)responseCode;

            //byte checksum = readBuffer [readBuffer.Length-1];
            byte checksum = readBuffer[4 + length];

            byte calculatedChecksum = CalcCheckSum(readBuffer, 4, 4 + length);

            validateChecksum(checksum, calculatedChecksum);

            return response;
        }

        /// <summary>
        /// Before you can exchange data with a MiFare chip, the transponder has to be activated (or
        /// „selected“ in the ISO14443 language). Card will be in active state after this call.
        /// Note that something isn't right about the SAK byte, it appears to be hardcoded to 9 from
        /// the reader and *not* coming from the card being communicated with!
        /// </summary>
        /// <returns>
        /// The mifare select.
        /// </returns>
        /// <param name='serialNo'>
        /// Serial no.
        /// </param>
        private SelectResponse sendMifareSelect(uint serialNo)
        {
            byte[] readBuffer = new byte[64];
            int offset = 0;
            int bytesToRead = 0;

            WriteCommand(Command.MIFARE_SELECT, NODE_BROADCAST, BitConverter.GetBytes(serialNo));

            //Console.WriteLine ("Card SerialNo: " + BitConverter.GetBytes(serialNo).ToHex() );

            // Naive busy way. Rewrite to consumer stream pull!
            do
            {
                Thread.Sleep(50);
                bytesToRead = port.BytesToRead;
            } while (bytesToRead == 0);

            // TODO: Use callback, make response parser able to read lazily from input stream
            int bytesRead = port.Read(readBuffer, offset, bytesToRead);

            // Evaluate data in buffer

            //Console.WriteLine ("Data received: {0} )", readBuffer.Subset(0,bytesRead).ToHex ());

            SelectResponse response = new SelectResponse();

            ushort length = readBuffer.Subset(2, 2).ToUInt16();
            //Console.WriteLine ("Length: {0} ( {1})", length, readBuffer.Subset (2, 2).ToHex ());

            ushort nodeID = readBuffer.Subset(4, 2).ToUInt16();
            //Console.WriteLine ("NodeID: {0} ( {1})", nodeID, readBuffer.Subset (4, 2).ToHex ());
            response.NodeId = nodeID;

            ushort commandCode = readBuffer.Subset(6, 2).ToUInt16();
            //Console.WriteLine ("Command: {0} ( {1})", Enum.GetName (typeof(CommandCode), commandCode), readBuffer.Subset (6, 2).ToHex ());
            response.Command = (Command)commandCode;

            byte responseCode = readBuffer[8];

            //Console.WriteLine ("Response Code: {0} ( {1})", Enum.GetName (typeof(ResponseCode), responseCode), responseCode.ToHex ());
            response.Response = (Response)responseCode;

            response.SAKByte = readBuffer[9];

            //byte checksum = readBuffer [readBuffer.Length-1]; // Hov... skal da ikke lave checksum indtil hele længden af bufferen, kun den del hvor vi har data i?!
            byte checksum = readBuffer[4 + length];

            byte calculatedChecksum = CalcCheckSum(readBuffer, 4, 4 + length);

            validateChecksum(checksum, calculatedChecksum);

            return response;
        }

        private MifareResponse sendHalt()
        {
            byte[] readBuffer = new byte[64];
            int offset = 0;
            int bytesToRead = 0;

            WriteCommand(Command.MIFARE_HLTA, NODE_BROADCAST);

            // Naive busy way. Rewrite to consumer stream pull!
            do
            {
                Thread.Sleep(50);
                bytesToRead = port.BytesToRead;
            } while (bytesToRead == 0);

            // TODO: Use callback, make response parser able to read lazily from input stream
            int bytesRead = port.Read(readBuffer, offset, bytesToRead);

            // Evaluate data in buffer

            //Console.WriteLine ("Data received: {0} )", readBuffer.Subset(0,bytesRead).ToHex ());

            SelectResponse response = new SelectResponse();

            ushort length = readBuffer.Subset(2, 2).ToUInt16();
            //Console.WriteLine ("Length: {0} ( {1})", length, readBuffer.Subset (2, 2).ToHex ());

            ushort nodeID = readBuffer.Subset(4, 2).ToUInt16();
            //Console.WriteLine ("NodeID: {0} ( {1})", nodeID, readBuffer.Subset (4, 2).ToHex ());
            response.NodeId = nodeID;

            ushort commandCode = readBuffer.Subset(6, 2).ToUInt16();
            //Console.WriteLine ("Command: {0} ( {1})", Enum.GetName (typeof(CommandCode), commandCode), readBuffer.Subset (6, 2).ToHex ());
            response.Command = (Command)commandCode;

            byte responseCode = readBuffer[8];

            //Console.WriteLine ("Response Code: {0} ( {1})", Enum.GetName (typeof(ResponseCode), responseCode), responseCode.ToHex ());
            response.Response = (Response)responseCode;

            //byte checksum = readBuffer [readBuffer.Length-1]; // Hov... skal da ikke lave checksum indtil hele længden af bufferen, kun den del hvor vi har data i?!
            byte checksum = readBuffer[4 + length];

            byte calculatedChecksum = CalcCheckSum(readBuffer, 4, 4 + length);

            validateChecksum(checksum, calculatedChecksum);

            return response;
        }

        /// <summary>
        /// This document has some great explanations about how the auth mechanisms work on Mifare cards:
        /// http://www.metratec.com/fileadmin/docs/en/documentation/metraTec_MiFare_Protocol-Guide.pdf
        /// 
        /// </summary>
        /// <returns>
        /// The auth.
        /// </returns>
        /// <param name='blockNo'>
        /// Block no.
        /// </param>
        /// <param name='authCode'>
        /// Auth code.
        /// </param>
        private MifareResponse sendAuth(byte blockNo, AuthMode authMode, byte[] authCode)
        {
            byte[] readBuffer = new byte[64];
            int offset = 0;
            int bytesToRead = 0;

            WriteCommand(Command.MIFARE_AUTHENTICATION2, NODE_BROADCAST,
                          new byte[] { (byte)authMode, blockNo }.Merge(authCode));

            // Naive busy way. Rewrite to consumer stream pull!
            do
            {
                Thread.Sleep(10);
                bytesToRead = port.BytesToRead;
            } while (bytesToRead == 0);

            // TODO: Use callback, make response parser able to read lazily from input stream
            int bytesRead = port.Read(readBuffer, offset, bytesToRead);

            // Evaluate data in buffer

            //Console.WriteLine ("Data received: {0} )", readBuffer.Subset(0,bytesRead).ToHex ());

            SelectResponse response = new SelectResponse();

            ushort length = readBuffer.Subset(2, 2).ToUInt16();
            //Console.WriteLine ("Length: {0} ( {1})", length, readBuffer.Subset (2, 2).ToHex ());

            ushort nodeID = readBuffer.Subset(4, 2).ToUInt16();
            //Console.WriteLine ("NodeID: {0} ( {1})", nodeID, readBuffer.Subset (4, 2).ToHex ());
            response.NodeId = nodeID;

            ushort commandCode = readBuffer.Subset(6, 2).ToUInt16();
            //Console.WriteLine ("Command: {0} ( {1})", Enum.GetName (typeof(CommandCode), commandCode), readBuffer.Subset (6, 2).ToHex ());
            response.Command = (Command)commandCode;

            byte responseCode = readBuffer[8];

            //Console.WriteLine ("Response Code: {0} ( {1})", Enum.GetName (typeof(ResponseCode), responseCode), responseCode.ToHex ());
            response.Response = (Response)responseCode;

            byte checksum = readBuffer[4 + length];

            byte calculatedChecksum = CalcCheckSum(readBuffer, 4, 4 + length);

            validateChecksum(checksum, calculatedChecksum);

            return response;
        }


        private ReadResponse sendRead(byte blockNo)
        {
            byte[] readBuffer = new byte[64];
            int offset = 0;
            int bytesToRead = 0;

            WriteCommand(Command.MIFARE_READ, NODE_BROADCAST, blockNo);

            // Naive busy way. Rewrite to consumer stream pull!
            do
            {
                Thread.Sleep(10);
                bytesToRead = port.BytesToRead;
            } while (bytesToRead == 0);

            // TODO: Use callback, make response parser able to read lazily from input stream
            int bytesRead = port.Read(readBuffer, offset, bytesToRead);

            // Evaluate data in buffer

            //Console.WriteLine ("Data received: {0} )", readBuffer.Subset(0,bytesRead).ToHex ());

            ReadResponse response = new ReadResponse();

            ushort length = readBuffer.Subset(2, 2).ToUInt16();
            //Console.WriteLine ("Length: {0} ( {1})", length, readBuffer.Subset (2, 2).ToHex ());

            ushort nodeID = readBuffer.Subset(4, 2).ToUInt16();
            //Console.WriteLine ("NodeID: {0} ( {1})", nodeID, readBuffer.Subset (4, 2).ToHex ());
            response.NodeId = nodeID;

            ushort commandCode = readBuffer.Subset(6, 2).ToUInt16();
            //Console.WriteLine ("Command: {0} ( {1})", Enum.GetName (typeof(CommandCode), commandCode), readBuffer.Subset (6, 2).ToHex ());
            response.Command = (Command)commandCode;

            byte responseCode = readBuffer[8];
            response.Response = (Response)responseCode;
            //Console.WriteLine ("Response Code: {0} ( {1})", Enum.GetName (typeof(ResponseCode), responseCode), responseCode.ToHex ());

            response.Data = readBuffer.Subset(9, 16);

            byte checksum = readBuffer[4 + length];

            byte calculatedChecksum = CalcCheckSum(readBuffer, 4, 4 + length);

            validateChecksum(checksum, calculatedChecksum);

            return response;
        }


        private static int getBlockCount(MifareType mifareType)
        {
            if (mifareType == MifareType.MIFARE_CLASSIC_4K_S70)
            {
                return 256;
            }
            else
            {
                return 64;
            }
            // TODO: Add other types
        }

        private void validateChecksum(byte checksum, byte calculatedChecksum)
        {
            if (checksum != calculatedChecksum)
            {
                throw new InvalidChecksumException(String.Format("Invalid checksum, expected {0} but got {1}", checksum, calculatedChecksum));
            }
        }

        static private byte CalcCheckSum(byte[] PacketData)
        {
            return CalcCheckSum(PacketData, 0, PacketData.Length);
        }

        static private byte CalcCheckSum(byte[] data, int start, int end)
        {
            byte checksum = 0x00;

            for (int offset = start; offset < end; offset++)
            {
                checksum ^= data[offset];
            }

            return checksum;
        }

        private void WriteCommand(Command commandCode, ushort nodeId)
        {
            WriteCommand(commandCode, nodeId, new byte[0]);
        }

        private void WriteCommand(Command commandCode, ushort nodeId, byte data)
        {
            WriteCommand(commandCode, nodeId, new byte[1] { data });
        }

        private void WriteCommand(Command commandCode, ushort nodeId, byte[] data)
        {
            Write(MAGIC_BYTES);

            Write((ushort)(data.Length + 5));

            byte[] nodeIdCommandAndData = BitConverter.GetBytes(nodeId).Merge(
                    BitConverter.GetBytes((ushort)commandCode),
                    data
            );

            // The ER301 hardware relies on escaping 0xAA sequences to 0xAA00 sequences
            nodeIdCommandAndData = nodeIdCommandAndData.ReplaceBytes(
                new byte[] { 0xaa },
                new byte[] { 0xaa, 0x00 });

            //Console.WriteLine ("NODE_ID, COMMAND and <DATA>: {0}", nodeIdCommandAndData.ToHex ());

            Write(nodeIdCommandAndData);

            byte CheckSum = CalcCheckSum(nodeIdCommandAndData);

            //Console.WriteLine ("CHECKSUM:");

            Write(CheckSum);
        }

        private void Write(byte data)
        {
            Write(new byte[] { data });
        }

        private void Write(ushort data)
        {
            Write(BitConverter.GetBytes(data));
        }

        // TODO: Rewrite so that all transmissions occur out of one allocated buffer, where only the length decides
        // how much is sent and what is discarded. (less GC, more effecient and easier handling of the AA escapes) 
        private void Write(byte[] data)
        {
            //Console.WriteLine( "Writing: {0})", data.ToHex () );
            port.Write(data, 0, data.Length);
        }


    }
}
