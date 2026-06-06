namespace NetClajServer.Datastructures;

public class UtfDataFormatException(string? message) : Exception(message);

public static class JavaUtfReadWriteExtension
{
    public static void WriteJavaUtf(this BinaryWriter w, string s)
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

        var bytearr = new byte[utflen + 2];

        var count = 0;
        bytearr[count++] = (byte)((utflen >>> 8) & 0xFF);
        bytearr[count++] = (byte)((utflen >>> 0) & 0xFF);

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
        
        w.Write(bytearr, 0, utflen + 2);
    }
}