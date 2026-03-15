namespace TrinityCore.GameClient.Net.Protocol.Crypto;

internal sealed class Arc4
{
    private readonly byte[] _state = new byte[256];
    private byte _x;
    private byte _y;

    public void Process(byte[] buffer, int start, int count)
    {
        for (var i = 0; i < count; i++)
        {
            _x = (byte)(_x + 1);
            _y = (byte)(_state[_x] + _y);
            (_state[_x], _state[_y]) = (_state[_y], _state[_x]);
            var xorIndex = (byte)(_state[_x] + _state[_y]);
            buffer[start + i] ^= _state[xorIndex];
        }
    }

    public static Arc4 Create(byte[] key)
    {
        var arc4 = new Arc4();
        arc4.Initialize(key);
        return arc4;
    }

    private void Initialize(byte[] key)
    {
        for (var i = 0; i < 256; i++)
        {
            _state[i] = (byte)i;
        }

        byte index1 = 0;
        byte index2 = 0;
        for (var i = 0; i < 256; i++)
        {
            index2 = (byte)(key[index1] + _state[i] + index2);
            (_state[i], _state[index2]) = (_state[index2], _state[i]);
            index1 = (byte)((index1 + 1) % key.Length);
        }
    }
}
