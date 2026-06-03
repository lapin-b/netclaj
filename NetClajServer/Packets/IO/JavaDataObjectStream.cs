namespace NetClajServer.Packets.IO;

public class UtfDataFormatException(string? message) : Exception(message);

public class JavaDataObjectStream
{
    public static string ReadUtf(ReadOnlySpan<byte> bytearr, int utflen)
    {
        var chararr = new char[utflen * 2];

        int c, char2, char3;
        int count = 0;
        int chararrCount = 0;

        while (count < utflen)
        {
            c = bytearr[count] & 0xff;
            if (c > 127) break;
            count++;
            chararr[chararrCount++] = (char)c;
        }

        while (count < utflen)
        {
            c = bytearr[count] & 0xff;

            switch (c >> 4)
            {
                case 0 or 1 or 2 or 3 or 4 or 5 or 6 or 7:
                    /* 0xxxxxxx*/
                    count++;
                    chararr[chararrCount++] = (char)c;
                    break;

                case 12 or 13:
                    /* 110x xxxx   10xx xxxx*/
                    count += 2;
                    if (count > utflen)
                        throw new UtfDataFormatException(
                            "malformed input: partial character at end");
                    char2 = bytearr[count - 1];
                    if ((char2 & 0xC0) != 0x80)
                        throw new UtfDataFormatException(
                            "malformed input around byte " + count);
                    chararr[chararrCount++] = (char)(((c & 0x1F) << 6) | (char2 & 0x3F));
                    break;

                case 14:
                    /* 1110 xxxx  10xx xxxx  10xx xxxx */
                    count += 3;
                    if (count > utflen)
                        throw new UtfDataFormatException(
                            "malformed input: partial character at end");
                    char2 = bytearr[count - 2];
                    char3 = bytearr[count - 1];
                    if ((char2 & 0xC0) != 0x80 || (char3 & 0xC0) != 0x80)
                        throw new UtfDataFormatException(
                            "malformed input around byte " + (count - 1));
                    chararr[chararrCount++] = (char)(((c & 0x0F) << 12) |  ((char2 & 0x3F) << 6) | ((char3 & 0x3F) << 0));
                    break;
                
                default:
                    /* 10xx xxxx,  1111 xxxx */
                    throw new UtfDataFormatException("malformed input around byte " + count);
            }
        }

        // The number of chars produced may be less than utflen
        return new string(chararr, 0, chararrCount);
    }

    public static byte[] EncodeUtf(string s)
    {
        var strlen = s.Length;
        var utflen = strlen; // optimized for ASCII
        int i;
        
        for(i = 0; i < strlen; i++){
            int c = s[i];
            if(c >= 0x80 || c == 0)
                utflen += c >= 0x800 ? 2 : 1;
        }

        if(utflen > 65535 || /* overflow */ utflen < strlen)
            throw new UtfDataFormatException("encoded string too long");

        var bytearr = new byte[utflen];
        var count = 0;

        for(i = 0; i < strlen; i++){ // optimized for initial run of ASCII
            int c = s[i];
            if(c >= 0x80 || c == 0) break;
            bytearr[count++] = (byte)c;
        }

        for(; i < strlen; i++){
            int c = s[i];
            if(c < 0x80 && c != 0){
                bytearr[count++] = (byte)c;
            }else if(c >= 0x800){
                bytearr[count++] = (byte)(0xE0 | ((c >> 12) & 0x0F));
                bytearr[count++] = (byte)(0x80 | ((c >> 6) & 0x3F));
                bytearr[count++] = (byte)(0x80 | ((c >> 0) & 0x3F));
            }else{
                bytearr[count++] = (byte)(0xC0 | ((c >> 6) & 0x1F));
                bytearr[count++] = (byte)(0x80 | ((c >> 0) & 0x3F));
            }
        }

        return bytearr;
    }
}