using System;
using System.Drawing;

class QR {
    const string SIZE_TABLE = "D01A01K01G01J01D01V01P01T01I01P02L02L02N01J04T02R02T01P04L04J04L02V04R04L04N02T05L06P04R02T06P06P05X02R08N08T05L04V08R08X05N04R11V08P08R04V11T10P09T04P16R12R09X04R16N16R10P06R18X12V10R06X16R17V11V06V19V16T13X06V21V18T14V07T25T21T16V08V25X20T17V08X25V23V17V09R34X23V18X09X30X25V20X10X32X27V21T12X35X29V23V12X37V34V25X12X40X34V26X13X42X35V28X14X45X38V29X15X48X40V31X16X51X43V33X17X54X45V35X18X57X48V37X19X60X51V38X19X63X53V40X20X66X56V43X21X70X59V45X22X74X62V47X24X77X65V49X25X81X68";
    const string QRALNUM = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";

    readonly byte[] EXP_LOG = new byte[513];
    const int LOG_BEGIN = 256;

    int mEncIdx;
    byte[] mEncoded;
    byte[] mQrData;
    byte[] mQrMask;

    QR() {
        // generate reed solomon expTable and logTable
        // QR uses GF256(0x11d) // 0x11d=285 => x^8 + x^4 + x^3 + x^2 + 1
        const int poly = 0x11d;
        for (long y = 0, x = 1; y < 256; y++) {
            EXP_LOG[y] = (byte)x;             // expTable
            EXP_LOG[x + LOG_BEGIN] = (byte)y; // logTable
            x <<= 1;
            if (x > 255) {
                x ^= poly;
            }
        }
    }

    int rsprod(int a, int b) {
        if (a > 0 && b > 0) {
            var idx = EXP_LOG[LOG_BEGIN + a] + EXP_LOG[LOG_BEGIN + b];
            idx %= 255;
            return EXP_LOG[idx];
        } else {
            return 0;
        }
    }

    void generateRS(int size, int len, int blocks) {
        for (int v_x = 0; v_x < len; v_x++) {
            mEncoded[v_x + size] = 0;
        }

        // qr code has first x blocks shorter than lasts
        var v_bs = size / blocks;    // shorter block size
        var v_es = len / blocks;     // ecc block size
        var remain = size % blocks;  // remain bytes
        var v_b2c = blocks - remain; // on block number v_b2c
        var v_ply = new byte[v_es + 1];
        v_ply[0] = 1;

        for (int v_x = 1, v_z = 0; v_x <= v_es; v_x++, v_z++) {
            int pa, pb;
            v_ply[v_x] = v_ply[v_x - 1];
            for (int v_y = v_x - 1; v_y > 0; v_y--) {
                pb = EXP_LOG[v_z];
                pa = v_ply[v_y];
                var rp = rsprod(pa, pb);
                v_ply[v_y] = (byte)(v_ply[v_y - 1] ^ rp);
            }
            pa = v_ply[0];
            pb = EXP_LOG[v_z];
            v_ply[0] = (byte)rsprod(pa, pb);
        }

        for (int v_b = 0; v_b <= blocks - 1; v_b++) {
            var vpo = v_b * v_es + size; // ECC start
            var vdo = v_b * v_bs;        // data start
            if (v_b > v_b2c) {
                vdo += v_b - v_b2c; // x longers before
            }
            // generate "nc" checkwords in the array
            var v_z = v_bs;
            if (v_b >= v_b2c) {
                v_z++;
            }
            for (int v_x = 0; v_x < v_z; v_x++) {
                var pa = mEncoded[vpo] ^ mEncoded[vdo + v_x];
                for (int v_a = v_es, v_y = vpo; v_a > 0; v_a--, v_y++) {
                    var pb = v_ply[v_a - 1];
                    var rp = rsprod(pa, pb);
                    if (v_a == 0) {
                        mEncoded[v_y] = (byte)rp;
                    } else {
                        mEncoded[v_y] = (byte)(mEncoded[v_y + 1] ^ rp);
                    }
                }
            }
        }
    }

    void putBits(int value, int len) {
        if (len > 56) {
            return;
        }
        var arr = new byte[7];
        var dw = (double)value;
        if (len < 56) {
            dw *= (long)1 << (56 - len);
        }
        for (int i = 0; i < 6 && dw > 0; i++) {
            var w = (long)dw >> 48;
            arr[i] = (byte)(w % 256);
            dw -= w << 48;
            dw *= 256;
        }
        mEncIdx = putBits(mEncoded, arr, len, mEncIdx);
    }

    int putBits(byte[] output, byte[] input, int len, int offsetBits = 0) {
        var offset_b = offsetBits % 8;
        var offset_i = offsetBits / 8;
        for (int i = 0, l = len; 0 < l; i++, l -= 8) {
            int w;
            if (i < input.Length) {
                w = input[i];
            } else {
                w = 0;
            }
            if (l < 8) {
                w &= 256 - (1 << (8 - l));
            }
            if (offset_b > 0) {
                w *= 1 << (8 - offset_b);
                output[offset_i] |= (byte)(w >> 8);
                output[offset_i + 1] |= (byte)w;
            } else {
                output[offset_i] |= (byte)w;
            }
            if (l < 8) {
                offsetBits += l;
                l = 0; // break;
            } else {
                offsetBits += 8;
                offset_i++;
            }
        }
        return offsetBits;
    }

    int countBits(int value) {
        int v, n;
        for (v = 1, n = 0; v <= value; v <<= 1, n++) ;
        return n;
    }

    // padding 0xEC,0x11,0xEC,0x11...
    // TYPE_INFO_MASK_PATTERN = 0x5412
    // TYPE_INFO_POLY = 0x537  [(ecLevel << 3) | maskPattern] : 5 + 10 = 15 bitu
    // VERSION_INFO_POLY = 0x1f25 : 5 + 12 = 17 bitu
    void bchCalc(ref int data, int poly) {
        var b = countBits(poly) - 1;
        if (data == 0) {
            //data = poly
            return;
        }
        var x = data << b;
        var rv = x;
        while (true) {
            var n = countBits(rv);
            if (n <= b) {
                break;
            }
            rv ^= poly << (n - b - 1);
        }
        data = x + rv;
    }

    bool putQrBitWithMask(int row, int col, int flag) {
        var idx = row * 24 + col / 8; // 24 bytes per row
        if (idx >= mQrData.Length || idx < 0) {
            return false;
        }
        var bit = 1 << (col % 8);
        var mask = mQrMask[idx];
        if (0 == (mask & bit)) {
            if (flag != 0) {
                mQrData[idx] |= (byte)bit;
            }
            return true;
        }
        return false;
    }

    bool putQrBit(int row, int col, int flag) {
        var idx = row * 24 + col / 8; // 24 bytes per row
        if (idx >= mQrData.Length || idx < 0) {
            return false;
        }
        var bit = 1 << (col % 8);
        if (0 == flag) {
            mQrData[idx] &= (byte)(0xFF - bit); // reset
        } else {
            mQrData[idx] |= (byte)bit; // set
        }
        mQrMask[idx] |= (byte)bit; // mask
        return true;
    }

    void maskQrBit(int input, int bits, int row, int col) {
        // max 8 bites wide
        if (bits > 8 || bits < 1) {
            return;
        }
        bool x;
        for (int i = 1 << (bits - 1), c = col; 0 < i; i >>= 1, c++) {
            x = putQrBit(row, c, input & i);
        }
    }

    void maskQrBit(byte[] input, int bits, int row, int col) {
        // max 8 bites wide
        if (bits > 8 || bits < 1) {
            return;
        }
        bool x;
        for (int j = 0, r = row; j < input.Length; j++, r++) {
            var w = (int)input[j];
            for (int i = 1 << (bits - 1), c = col; 0 < i; i >>= 1, c++) {
                x = putQrBit(r, c, w & i);
            }
        }
    }

    void fillQrBit(int size, int blocks, int pdlen, int ptlen) {
        // vyplni pole parr (psiz x 24 bytes) z pole pb pdlen = pocet dbytes, pblocks = bloku, ptlen celkem
        // podle logiky qr_kodu - s prokladem

        // qr code has first x blocks shorter than lasts but datamatrix has first longer and shorter last
        var vds = pdlen / blocks;              // shorter data block size
        var ves = (ptlen - pdlen) / blocks;    // ecc block size
        var vdnlen = vds * blocks;             // potud jsou databloky stejne velike
        var vsb = blocks - (pdlen % blocks);// mensich databloku je ?

        var c = size - 1;
        var r = c;    // start position on right lower corner
        var smer = 0; // nahoru :  3 <- 2 10  dolu: 1 <- 0  32
                      //           1 <- 0 10        3 <- 2  32
        var count = 1;
        var encoded = mEncoded[0];
        var vx = 0;
        while (c >= 0 && count <= ptlen) {
            if (putQrBitWithMask(r, c, encoded & 0x80)) {
                vx++;
                if (vx == 8) {
                    encoded = getEncodedData(vds, ves, vsb,
                        pdlen, ptlen, vdnlen,
                        count, blocks
                    ); // first byte
                    count++;
                    vx = 0;
                } else {
                    encoded = (byte)((encoded << 1) & 0xFF);
                }
            }
            switch (smer) {
            case 0:
            case 2: // nahoru nebo dolu a jsem vpravo
                c--;
                smer++;
                break;
            case 1: // nahoru a jsem vlevo
                if (r == 0) { // nahoru uz to nejde
                    c--;
                    if (c == 6 && size >= 21) {
                        c--; // preskoc sync na sloupci 6
                    }
                    smer = 2; // a jedeme dolu
                } else {
                    c++;
                    r--;
                    smer = 0; // furt nahoru
                }
                break;
            case 3: // dolu a jsem vlevo
                if (r == (size - 1)) { // dolu uz to nepude
                    c--;
                    if (c == 6 && size >= 21) {
                        c--; // preskoc sync na sloupci 6
                    }
                    smer = 0;
                } else {
                    c++;
                    r++;
                    smer = 2;
                }
                break;
            }
        }
    }

    byte getEncodedData(int vds, int ves, int vsb,
        int pdlen, int ptlen, int vdnlen,
        int count, int pblocks
    ) {
        /* next byte
            plen = 14 pbl = 3   => 1x4 + 2x5 (v_b2c = 3 - 2 = 1; v_bs1 = 4)
                v_b = 0 => v_last = 0 + 4 * 3 - 2 = 10 => 1..12 by 3   1,4,7,10
                v_b = 1 => v_last = 1 + 4 * 3     = 13 => 2..13 by 3   2,5,8,11,13
                v_b = 2 => v_last = 2 + 4 * 3     = 14 => 3..14 by 3   3,6,9,12,14
            plen = 15 pbl = 3   => 3x5 (v_b2c = 3; v_bs1 = 5)
                v_b = 0 => v_last = 0 + 5 * 3 - 2 = 13 => 1..13 by 3   1,4,7,10,13
                v_b = 1 => v_last = 1 + 5 * 3 - 2 = 14 => 2..14 by 3   2,5,8,11,14
                v_b = 2 => v_last = 2 + 5 * 3 - 2 = 15 => 3..15 by 3   3,6,9,12,15
        */
        if (count < pdlen) { // Datovy byte
            var wa = count;
            if (count >= vdnlen) {
                wa += vsb;
            }
            var wb = wa % pblocks;
            wa /= pblocks;
            if (wb > vsb) {
                wa += wb - vsb;
            }
            return mEncoded[vds * wb + wa];
        }
        if (count < ptlen) { // ecc byte
            var wa = count - pdlen;   // kolikaty ecc 0..x
            var wb = wa % pblocks; // z bloku
            wa /= pblocks;         // kolikaty
            return mEncoded[pdlen + ves * wb + wa];
        }
        return 0;
    }

    // Black If 0: (c+r) mod 2 = 0    4: ((r div 2) + (c div 3)) mod 2 = 0
    //          1: r mod 2 = 0        5: (c*r) mod 2 + (c*r) mod 3 = 0
    //          2: c mod 3 = 0        6: ((c*r) mod 2 + (c*r) mod 3) mod 2 = 0
    //          3: (c+r) mod 3 = 0    7: ((c+r) mod 2 + (c*r) mod 3) mod 2 = 0
    int xormask(int size, int mod, bool final) {
        int c, r, i;
        var warr = new byte[size * 24];
        for (r = 0; r < size; r++) {
            var m = 1;
            var ix = 24 * r;
            warr[ix] = mQrData[ix];
            for (c = 0; c < size; c++) {
                if (0 == (mQrMask[ix] & m)) { // nemaskovany
                    switch (mod) {
                    case 0:
                        i = (c + r) % 2; break;
                    case 1:
                        i = r % 2; break;
                    case 2:
                        i = c % 3; break;
                    case 3:
                        i = (c + r) % 3; break;
                    case 4:
                        i = ((r / 2) + (c / 3)) % 2; break;
                    case 5:
                        i = (c * r) % 2 + (c * r) % 3; break;
                    case 6:
                        i = ((c * r) % 2 + (c * r) % 3) % 2; break;
                    case 7:
                        i = ((c + r) % 2 + (c * r) % 3) % 2; break;
                    default:
                        i = 0; break;
                    }
                    if (i == 0) {
                        warr[ix] ^= (byte)m;
                    }
                }
                if (m == 128) {
                    m = 1;
                    if (final) {
                        mQrData[ix] = warr[ix];
                    }
                    ix++;
                    warr[ix] = mQrData[ix];
                } else {
                    m <<= 1;
                }
            }
            if (m != 128 && final) {
                mQrData[ix] = warr[ix];
            }
        }
        if (final) {
            return 0;
        }

        // score computing
        // a) adjacent modules colors in row or column 5+i mods = 3 + i penatly
        // b) block same color MxN = 3*(M-1)*(N-1) penalty OR every 2x2 block penalty + 3
        // c) 4:1:1:3:1:1 or 1:1:3:1:1:4 in row or column = 40 penalty rmks: 00001011101 or 10111010000 = &H05D or &H5D0
        // d) black/light ratio : k=(abs(ratio% - 50) DIV 5) means 10*k penalty
        var score = 0;
        var black_count = 0;
        int[,] cols = new int[2, size];
        for (r = 0; r < size; r++) {
            var m = 1;
            var ix = 24 * r;
            var rp = 0;
            var rc = 0;
            for (c = 0; c < size; c++) {
                rp = (rp & 0x3FF) << 1; // only last 12 bits
                cols[1, c] = (cols[1, c] & 0x3FF) << 1;
                if (0 == (warr[ix] & m)) {
                    if (rc > 0) { // in row x black
                        if (rc >= 5) {
                            score += rc - 2;
                        }
                        rc = 0;
                    }
                    rc--; // one more white
                    if (cols[0, c] > 0) { // color changed
                        if (cols[0, c] >= 5) {
                            score += cols[0, c] - 2;
                        }
                        cols[0, c] = 0;
                    }
                    cols[0, c]--; // one more white
                } else {
                    if (rc < 0) { // in row x whites
                        if (rc <= -5) {
                            score -= rc + 2;
                        }
                        rc = 0;
                    }
                    rc++; // one more black
                    if (cols[0, c] < 0) { // color changed
                        if (cols[0, c] <= -5) {
                            score -= cols[0, c] + 2;
                        }
                        cols[0, c] = 0;
                    }
                    cols[0, c]++; // one more black
                    rp |= 1;
                    cols[1, c] |= 1;
                    black_count++;
                }
                if (c > 0 && r > 0) { // penalty block 2x2
                    i = rp & 3; // current row pair
                    if ((cols[1, c - 1] & 3) >= 2) {
                        i += 8;
                    }
                    if ((cols[1, c] & 3) >= 2) {
                        i += 4;
                    }
                    if (i == 0 || i == 15) {
                        score += 3;
                        // b) penalty na 2x2 block same color
                    }
                }
                if (c >= 10 && (rp == 0x5D || rp == 0x5D0)) { // penalty pattern c in row
                    score += 40;
                }
                if (r >= 10 && (cols[1, c] == 0x5D || cols[1, c] == 0x5D0)) { // penalty pattern c in column
                    score += 40;
                }
                // next mask / byte
                if (m == 128) {
                    m = 1;
                    ix++;
                } else {
                    m <<= 1;
                }
            }
            if (rc <= -5) {
                score -= rc + 2;
            }
            if (rc >= 5) {
                score += rc - 2;
            }
        }
        for (c = 0; c < size; c++) { // after last row count column blocks
            if (cols[0, c] <= -5) {
                score -= cols[0, c] + 2;
            }
            if (cols[0, c] >= 5) {
                score += cols[0, c] - 2;
            }
        }
        black_count = black_count * 100 / (size * size);
        black_count = Math.Abs(black_count - 50) / 5 * 10;
        //MsgBox "mask:" + pmod + " " + s(0) + "+" + s(1) + "+" + s(2) + "+" + s(3) + "+" + bl
        return score + black_count;
    }

    void addmm(int mode, int mask, int size) {
        var k = mode * 8 + mask;
        // poly: 101 0011 0111
        bchCalc(ref k, 0x537);
        //MsgBox "mask :" & hex(k,3) & " " & hex(k xor &H5412,3)
        k ^= 0x5412; // micro xor 0x4445
        var r = 0;
        var c = size - 1;
        bool x;
        for (int i = 0; i <= 14; i++) {
            var ch = k % 2;
            k >>= 1;
            x = putQrBit(r, 8, ch); // svisle fmtinfo UL - bity 0..5 SYNC 6,7 .... 8..14 dole
            x = putQrBit(8, c, ch); // vodorovne odzadu 0..7 ............ 8,SYNC,9..14
            c--;
            r++;
            if (i == 5) {
                r++; // preskoc sync vodorvny
            }
            if (i == 7) {
                c = 7;
                r = size - 7;
            }
            if (i == 8) {
                c--; // preskoc sync svisly
            }
        }
    }

    void setParams(int cap, int mode, int[] ecx_count, out int[] prop) {
        int size = 0, totby = 0;
        int syncs = 0, ccsiz = 0, ccblks = 0, ver = 0;
        //prop:
        // 1:version, 2:size, 3:ccs, 4:ccb,
        // 5:totby, 6-12:syncs(7), 13-15:versinfo(3)
        prop = new int[16];
        //mode:M=0,L=1,H=2,Q=3
        if (mode < 0 || mode > 3) {
            return;
        }
        for (int i = 1; i < prop.Length; i++) {
            prop[i] = 0;
        }
        var vs = (cap + 18 * ecx_count[1] + 17 * ecx_count[2] + 20 * ecx_count[3] + 7) / 8;
        if (mode == 0 && vs > 2334 ||
            mode == 1 && vs > 2956 ||
            mode == 2 && vs > 1276 ||
            mode == 3 && vs > 1666
        ) {
            return;
        }
        vs = (cap + 14 * ecx_count[1] + 13 * ecx_count[2] + 12 * ecx_count[3] + 7) / 8;
        for (ver = 1; ver <= 40; ver++) {
            if (ver == 10) {
                vs = (cap + 16 * ecx_count[1] + 15 * ecx_count[2] + 20 * ecx_count[3] + 7) / 8;
            }
            if (ver == 27) {
                vs = (cap + 18 * ecx_count[1] + 17 * ecx_count[2] + 20 * ecx_count[3] + 7) / 8;
            }
            size = 4 * ver + 17;
            var i = (ver - 1) * 12 + mode * 3;
            var s = SIZE_TABLE.Substring(i, 3);
            ccsiz = s.Substring(0, 1).ToCharArray()[0] - 65 + 7;
            ccblks = int.Parse(s.Substring(s.Length - 2, 2));
            if (ver == 1) {
                syncs = 0;
                totby = 26;
            } else {
                //syncs = ((Int(ver / 7) + 2) ^ 2) - 3
                syncs = (ver / 7) + 2;
                syncs *= syncs;
                syncs -= 3;
                totby = size - 1;
                totby = ((totby * totby) / 8) - (3 * syncs) - 24;
                if (ver > 6)
                    totby -= 4;
                if (syncs == 1)
                    totby--;
            }
            //MsgBox "ver:" & ver & " tot: " & totby & " dat:" & (totby - ccsiz * ccblks) & " need:" & j
            if (totby - ccsiz * ccblks >= vs) {
                break;
            }
        }
        if (ver > 1) {
            syncs = (ver / 7) + 2;
            prop[6] = 6;
            prop[5 + syncs] = size - 7;
            if (syncs > 2) {
                var i = (int)((size - 13) / 2 / (syncs - 1) + 0.7) * 2;
                prop[7] = prop[5 + syncs] - i * (syncs - 2);
                if (syncs > 3) {
                    for (int j = 3; j < syncs; j++) {
                        prop[5 + j] = prop[4 + j] + i;
                    }
                }
            }
        }
        prop[1] = ver;
        prop[2] = size;
        prop[3] = ccsiz;
        prop[4] = ccblks;
        prop[5] = totby;
        if (ver >= 7) {
            var v = ver;
            bchCalc(ref v, 0x1F25);
            prop[13] = (v >> 16) & 0xFF;
            prop[14] = (v >> 8) & 0xFF;
            prop[15] = v & 0xFF;
        }
    }

    int getProp(string input, int mode, out int[] prop, out int[,] eb) {
        var ecx_cnt = new int[4];
        var ecx_pos = new int[4];
        var ecx_count = new int[4];
        for (int i = 0; i < 4; i++) {
            ecx_cnt[i] = 0;
            ecx_pos[i] = 0;
            ecx_count[i] = 0;
        }
        eb = new int[20, 5];
        var ebcnt = 1;
        var utf8 = 0;
        for (int idx = 0; idx <= input.Length; idx++) {
            int m;
            int byte_count = 0;
            if (input.Length <= idx) {
                m = -5;
            } else {
                var chr = input.Substring(idx, 1).ToCharArray()[0];
                if (chr >= 0x1FFFFF) { // FFFF - 1FFFFFFF
                    byte_count = 4;
                    m = -1;
                } else if (chr >= 0x7FF) { // 7FF-FFFF
                    byte_count = 3;
                    m = -1;
                } else {
                    byte_count = 1;
                    m = -1;
                }
                //} else if (chr >= 128) {
                //    byte_count = 2;
                //    k = -1;
                //} else {
                //    byte_count = 1;
                //    k = QRALNUM.IndexOf(input.Substring(i, 1));
                //}
            }
            if (m < 0) { // bude byte nebo konec
                if (ecx_cnt[1] >= 9 || (m == -5 && ecx_cnt[1] == ecx_cnt[3])) { // Az dosud bylo mozno pouzitelne numeric
                    if ((ecx_cnt[2] - ecx_cnt[1]) >= 8 || (ecx_cnt[3] == ecx_cnt[2])) { // pred num je i pouzitelny alnum
                        if (ecx_cnt[3] > ecx_cnt[2]) { // Jeste pred alnum bylo byte
                            eb[ebcnt, 1] = 3;          // Typ byte
                            eb[ebcnt, 2] = ecx_pos[3]; // pozice
                            eb[ebcnt, 3] = ecx_cnt[3] - ecx_cnt[2]; // delka
                            ebcnt++;
                            ecx_count[3]++;
                        }
                        eb[ebcnt, 1] = 2;         // Typ alnum
                        eb[ebcnt, 2] = ecx_pos[2];
                        eb[ebcnt, 3] = ecx_cnt[2] - ecx_cnt[1]; // delka
                        ebcnt++;
                        ecx_count[2]++;
                        ecx_cnt[2] = 0;
                    } else if (ecx_cnt[3] > ecx_cnt[1]) { // byly bytes pred numeric
                        eb[ebcnt, 1] = 3;          // Typ byte
                        eb[ebcnt, 2] = ecx_pos[3]; // pozice
                        eb[ebcnt, 3] = ecx_cnt[3] - ecx_cnt[1]; // delka
                        ebcnt++;
                        ecx_count[3]++;
                    }
                } else if ((ecx_cnt[2] >= 8) || (m == -5 && ecx_cnt[2] == ecx_cnt[3])) { // Az dosud bylo mozno pouzitelne alnum
                    if (ecx_cnt[3] > ecx_cnt[2]) { // Jeste pred alnum bylo byte
                        eb[ebcnt, 1] = 3;          // Typ byte
                        eb[ebcnt, 2] = ecx_pos[3]; // pozice
                        eb[ebcnt, 3] = ecx_cnt[3] - ecx_cnt[2]; // delka
                        ebcnt++;
                        ecx_count[3]++;
                    }
                    eb[ebcnt, 1] = 2;          // Typ alnum
                    eb[ebcnt, 2] = ecx_pos[2];
                    eb[ebcnt, 3] = ecx_cnt[2]; // delka
                    ebcnt++;
                    ecx_count[2]++;
                    ecx_cnt[3] = 0;
                    ecx_cnt[2] = 0; // vse zpracovano
                } else if (m == -5 && ecx_cnt[3] > 0) { // konec ale mam co ulozit
                    eb[ebcnt, 1] = 3;          // Typ byte
                    eb[ebcnt, 2] = ecx_pos[3]; // pozice
                    eb[ebcnt, 3] = ecx_cnt[3]; // delka
                    ebcnt++;
                    ecx_count[3]++;
                }
            }
            if (m == -5) {
                break;
            }
            if (m >= 0) { // Muzeme alnum
                if (m >= 10 && ecx_cnt[1] >= 12) { // Az dosud bylo mozno num
                    if ((ecx_cnt[2] - ecx_cnt[1]) >= 8 || (ecx_cnt[3] == ecx_cnt[2])) { // Je tam i alnum ktery stoji za to
                        if (ecx_cnt[3] > ecx_cnt[2]) { // Jeste pred alnum bylo byte
                            eb[ebcnt, 1] = 3;          // Typ byte
                            eb[ebcnt, 2] = ecx_pos[3]; // pozice
                            eb[ebcnt, 3] = ecx_cnt[3] - ecx_cnt[2]; // delka
                            ebcnt++;
                            ecx_count[3]++;
                        }
                        eb[ebcnt, 1] = 2;          // Typ alnum
                        eb[ebcnt, 2] = ecx_pos[2];
                        eb[ebcnt, 3] = ecx_cnt[2] - ecx_cnt[1]; // delka
                        ebcnt++;
                        ecx_count[2]++;
                        ecx_cnt[2] = 0; // vse zpracovano
                    } else if (ecx_cnt[3] > ecx_cnt[1]) { // Pred Num je byte
                        eb[ebcnt, 1] = 3;          // Typ byte
                        eb[ebcnt, 2] = ecx_pos[3]; // pozice
                        eb[ebcnt, 3] = ecx_cnt[3] - ecx_cnt[1]; // delka
                        ebcnt++;
                        ecx_count[3]++;
                    }
                    eb[ebcnt, 1] = 1;          // Typ numerix
                    eb[ebcnt, 2] = ecx_pos[1];
                    eb[ebcnt, 3] = ecx_cnt[1]; // delka
                    ebcnt++;
                    ecx_count[1]++;
                    ecx_cnt[1] = 0;
                    ecx_cnt[2] = 0;
                    ecx_cnt[3] = 0; // vse zpracovano
                }
                if (ecx_cnt[2] == 0) {
                    ecx_pos[2] = idx;
                }
                ecx_cnt[2]++;
            } else { // mozno alnum
                ecx_cnt[2] = 0;
            }
            if (m >= 0 && m < 10) { // muze byt numeric
                if (ecx_cnt[1] == 0) {
                    ecx_pos[1] = idx;
                }
                ecx_cnt[1]++;
            } else {
                ecx_cnt[1] = 0;
            }
            if (ecx_cnt[3] == 0) {
                ecx_pos[3] = idx;
            }
            ecx_cnt[3] += byte_count;
            utf8 += byte_count;
            if (ebcnt >= 16) { // Uz by se mi tri dalsi bloky stejne nevesli
                ecx_cnt[1] = 0;
                ecx_cnt[2] = 0;
            }
            //MsgBox "Znak:" & Mid(ptext,i,1) & "(" & k & ") ebn=" & ecx_pos(1) & "." & ecx_cnt(1) & " eba=" & ecx_pos(2) & "." & ecx_cnt(2) & " ebb=" & ecx_pos(3) & "." & ecx_cnt(3)
        }
        ebcnt--;

        var cap = 0;
        for (int i = 1; i <= ebcnt; i++) {
            switch (eb[i, 1]) {
            case 1:
                eb[i, 4] = (eb[i, 3] / 3) * 10 + (eb[i, 3] % 3) * 3 + ((eb[i, 3] % 3) > 0 ? 1 : 0);
                break;
            case 2:
                eb[i, 4] = (eb[i, 3] / 2) * 11 + (eb[i, 3] % 2) * 6;
                break;
            case 3:
                eb[i, 4] = eb[i, 3] * 8;
                break;
            }
            cap += eb[i, 4];
        }
        // UTF-8 is default not need ECI value - zxing cannot recognize
        //setParams(cap * 8 + utf8,mode,qrp);
        setParams(cap, mode, ecx_count, out prop);
        if (prop[1] <= 0) {
            // Too long;
            return 0;
        }
        return ebcnt;
    }

    string gen(string input, string options) {
        if (input == "") {
            // Not data
            return "";
        }

        // M=0,L=1,H=2,Q=3
        int mode;
        {
            var m = "M";
            var idx = options.IndexOf("mode=");
            if (0 <= idx) {
                m = options.Substring(idx + 5, 1);
            }
            mode = "MLHQ".IndexOf(m);
            if (mode < 0) {
                mode = 0;
            }
        }

        int[] prop;
        int[,] eb;
        var ebcnt = getProp(input, mode, out prop, out eb);
        var size = prop[2];

        mEncIdx = 0;
        mEncoded = new byte[prop[5] + 1];
        // byte mode (ASCII) all max 3200 bytes
        // mode indicator (1=num,2=AlNum,4=Byte,8=kanji,ECI=7)
        //      mode: Byte Alhanum  Numeric  Kanji
        // ver 1..9 :  8      9       11       8
        //   10..26 : 16     11       12      10
        //   27..40 : 16     13       14      12
        // UTF-8 is default not need ECI value - zxing cannot recognize
        // if utf8 > 0 Then
        //   k = &H700 + 26 ' UTF - 8 = 26; Win1250 = 21; 8859 - 2 = 4 viz http://strokescribe.com/en/ECI.html
        //   putBits(k, 12)
        // End If
        for (int i = 1; i <= ebcnt; i++) {
            switch (eb[i, 1]) {
            case 1: {
                var l = prop[1] < 10 ? 10 : (prop[1] < 27 ? 12 : 14);
                var v = (1 << l) + eb[i, 3];
                putBits(v, l + 4);
                break;
            }
            case 2: {
                var l = prop[1] < 10 ? 9 : (prop[1] < 27 ? 11 : 13);
                var v = (2 << l) + eb[i, 3];
                putBits(v, l + 4);
                break;
            }
            case 3: {
                var l = prop[1] < 10 ? 8 : 16;
                var v = (4 << l) + eb[i, 3];
                putBits(v, l + 4);
                break;
            }
            }
            var r = 0;
            var byte_count = 0;
            for (int idx = eb[i, 2]; byte_count < eb[i, 3]; idx++) {
                var chr = input.Substring(idx, 1).ToCharArray()[0];
                if (eb[i, 1] == 1) {
                    r = (r * 10) + ((chr - 0x30) % 10);
                    if ((byte_count % 3) == 2) {
                        putBits(r, 10);
                        r = 0;
                    }
                    byte_count++;
                } else if (eb[i, 1] == 2) {
                    r = (r * 45) + (QRALNUM.IndexOf(chr) % 45);
                    if ((byte_count % 2) == 1) {
                        putBits(r, 11);
                        r = 0;
                    }
                    byte_count++;
                } else {
                    if (chr > 0x1FFFFF) { // FFFF - 1FFFFFFF
                        var v = 0xF0 + ((chr >> 18) & 0x07);
                        putBits(v, 8);
                        v = 0x80 + ((chr >> 12) & 0x3F);
                        putBits(v, 8);
                        v = 0x80 + ((chr >> 6) & 0x3F);
                        putBits(v, 8);
                        v = 0x80 + (chr & 0x3F);
                        putBits(v, 8);
                        byte_count += 4;
                    } else if (chr > 0x7FF) { // 7FF-FFFF 3 bytes
                        var v = 0xE0 + ((chr >> 12) & 0x0F);
                        putBits(v, 8);
                        v = 0x80 + ((chr >> 6) & 0x3F);
                        putBits(v, 8);
                        v = 0x80 + (chr & 0x3F);
                        putBits(v, 8);
                        byte_count += 3;
                    } else if (chr > 0x7F) { // 2 bytes
                        var v = 0xC0 + ((chr >> 6) & 0x3F);
                        putBits(v, 8);
                        v = 0x80 + (chr & 0x3F);
                        putBits(v, 8);
                        byte_count += 2;
                    } else {
                        var v = chr & 0xFF;
                        putBits(v, 8);
                        byte_count++;
                    }
                }
            }
            switch (eb[i, 1]) {
            case 1:
                if (1 == (byte_count % 3)) {
                    putBits(r, 4);
                } else if (2 == (byte_count % 3)) {
                    putBits(r, 7);
                }
                break;
            case 2:
                if (1 == (byte_count % 2)) {
                    putBits(r, 6);
                }
                break;
            }
            //MsgBox "blk[" & i & "] t:" & eb(i,1) & "from " & eb(i,2) & " to " & eb(i,3) + eb(i,2) & " bits=" & mEncix
        }

        putBits(0, 4); // end of chain
        if ((mEncIdx % 8) != 0) { // round to byte
            putBits(0, 8 - (mEncIdx % 8));
        }
        // padding
        var enc_len = (prop[5] - prop[3] * prop[4]) * 8;
        if (mEncIdx > enc_len) {
            // Encode length error
            return "";
        }
        // padding 0xEC,0x11,0xEC,0x11...
        while (mEncIdx < enc_len) {
            putBits(-5103, 16);
        }
        // doplnime ECC
        var ecc_len = prop[3] * prop[4]; //ppoly, pmemptr , psize , plen , pblocks
        generateRS(prop[5] - ecc_len, ecc_len, prop[4]);
        //Call arr2hexstr(mEncoded)
        mEncIdx = prop[5];

        // Pole pro vystup
        mQrData = new byte[prop[2] * 24 + 24];
        mQrMask = new byte[prop[2] * 24 + 24];
        mQrMask[0] = 0;

        var qrsync1 = new byte[8];
        putBits(qrsync1, new byte[] { 0xFE, 0x82, 0xBA, 0xBA, 0xBA, 0x82, 0xFE, 0 }, 64);
        maskQrBit(qrsync1, 8, 0, 0);       // sync UL
        maskQrBit(0, 8, 8, 0);             // fmtinfo UL under - bity 14..9 SYNC 8
        maskQrBit(qrsync1, 8, 0, size - 7); // sync UR ( o bit vlevo )
        maskQrBit(0, 8, 8, size - 8);       // fmtinfo UR - bity 7..0
        maskQrBit(qrsync1, 8, size - 7, 0); // sync DL (zasahuje i do quiet zony)
        maskQrBit(0, 8, size - 8, 0);       // blank nad DL

        bool x;
        for (int i = 0; i <= 6; i++) {
            x = putQrBit(i, 8, 0);           // svisle fmtinfo UL - bity 0..5 SYNC 6,7
            x = putQrBit(i, size - 8, 0);     // svisly blank pred UR
            x = putQrBit(size - 1 - i, 8, 0); // svisle fmtinfo DL - bity 14..8
        }
        x = putQrBit(7, 8, 0);       // svisle fmtinfo UL - bity 0..5 SYNC 6,7
        x = putQrBit(7, size - 8, 0); // svisly blank pred UR
        x = putQrBit(8, 8, 0);       // svisle fmtinfo UL - bity 0..5 SYNC 6,7
        x = putQrBit(size - 8, 8, 1); // black dot DL
        if (prop[13] != 0 || prop[14] != 0) { // versioninfo
            // UR ver 0 1 2;3 4 5;...;15 16 17
            // LL ver 0 3 6 9 12 15;1 4 7 10 13 16; 2 5 8 11 14 17
            var v = 65536 * prop[13] + 256 * prop[14] + 1 * prop[15];
            var c = 0;
            var r = 0;
            for (int i = 0; i <= 17; i++) {
                var f = v % 2;
                x = putQrBit(r, size - 11 + c, f); // UR ver
                x = putQrBit(size - 11 + c, r, f); // DL ver
                c++;
                if (c > 2) {
                    c = 0;
                    r++;
                }
                f >>= 1;
            }
        }
        {
            var c = 1;
            for (int i = 8; i <= size - 9; i++) { // sync lines
                x = putQrBit(i, 6, c); // vertical on column 6
                x = putQrBit(6, i, c); // horizontal on row 6
                c = (c + 1) % 2;
            }
        }

        // other syncs
        var qrsync2 = new byte[5];
        putBits(qrsync2, new byte[] { 0x1F, 0x11, 0x15, 0x11, 0x1F }, 40);
        {
            var ch = 6;
            while (ch > 0 && prop[6 + ch] == 0) {
                ch--;
            }
            if (ch > 0) {
                for (int c = 0; c <= ch; c++) {
                    for (int r = 0; r <= ch; r++) {
                        // corners
                        if ((c != 0 || r != 0) && (c != ch || r != 0) && (c != 0 || r != ch)) {
                            maskQrBit(qrsync2, 5, prop[r + 6] - 2, prop[c + 6] - 2);
                        }
                    }
                }
            }
        }

        // vyplni pole parr (size x 24 bytes) z pole pb pdlen = pocet dbytes, pblocks = bloku, ptlen celkem
        fillQrBit(size, prop[4], prop[5] - prop[3] * prop[4], prop[5]);
        var mask = 8; // auto
        {
            var m = options.IndexOf("mask=");
            if (0 <= m) {
                mask = options.Substring(m + 5, 1).ToCharArray()[0];
            }
        }
        if (mask < 0 || mask > 7) {
            var m = 0;
            var s = -1;
            for (mask = 0; mask <= 7; mask++) {
                addmm(mode, mask, size);
                var score = xormask(size, mask, false);
                if (score < s || s == -1) {
                    s = score;
                    m = mask;
                }
            }
            //MessageBox.Show("best is " + m + " with score " + s);
            mask = m;
        }
        addmm(mode, mask, size);
        xormask(size, mask, true);

        var ascimatrix = "";
        for (int r = 0; r <= size; r += 2) {
            var score = 0;
            var s = 0;
            var v = 0;
            for (int c = 0; c <= size; c += 2) {
                if (0 == (c % 8)) {
                    v = mQrData[s + 24 * r];
                    if (r < size) {
                        score = mQrData[s + 24 * (r + 1)];
                    } else {
                        score = 0;
                    }
                    s++;
                }
                ascimatrix += ((char)('a' + (v % 4) + 4 * (score % 4))).ToString();
                v >>= 2;
                score >>= 2;
            }
            ascimatrix += "\n";
        }
        return ascimatrix;
    }

    static QR mInstance = null;

    public static Bitmap Draw(string data, float pitch, int para = 0) {
        if (null == mInstance) {
            mInstance = new QR();
        }
        var s = "mode=" + "MLQH".Substring(para % 4, 1);
        s = mInstance.gen(data, s);

        var x = 0.0f;
        var y = 0.0f;
        var m = 2 * pitch;
        var dm = m * 2;
        var a = 0.0f;
        var p = s;
        var b = p.Length;
        for (int n = 0; n < b; n++) {
            var w = p.Substring(n, 1).ToCharArray()[0] % 256;
            if (w >= 97 && w <= 112) {
                a += dm;
            } else if (w == 10 || n == b) {
                if (x < a) {
                    x = a;
                }
                y += dm;
                a = 0;
            }
        }

        if (x < 0.5) {
            return new Bitmap(1, 1);
        }

        var bmp = new Bitmap((int)(x + 0.5), (int)(y + 0.5));
        var g = Graphics.FromImage(bmp);
        x = 0;
        y = 0;
        for (int n = 0; n < b; n++) {
            var w = p.Substring(n, 1).ToCharArray()[0] % 256;
            if (w == '\n') {
                y += dm;
                x = 0;
            } else if (w >= 'a' && w <= 'p') {
                w -= 'a';
                switch (w) {
                case 1:
                    g.FillRectangle(Brushes.Black, x + 0, y + 0, m, m);
                    break;
                case 2:
                    g.FillRectangle(Brushes.Black, x + m, y + 0, m, m);
                    break;
                case 3:
                    g.FillRectangle(Brushes.Black, x + 0, y + 0, dm, m);
                    break;
                case 4:
                    g.FillRectangle(Brushes.Black, x + 0, y + m, m, m);
                    break;
                case 5:
                    g.FillRectangle(Brushes.Black, x + 0, y + 0, m, dm);
                    break;

                case 6:
                    g.FillRectangle(Brushes.Black, x + m, y + 0, m, m);
                    g.FillRectangle(Brushes.Black, x + 0, y + m, m, m);
                    break;
                case 7:
                    g.FillRectangle(Brushes.Black, x + 0, y + 0, dm, m);
                    g.FillRectangle(Brushes.Black, x + 0, y + m, m, m);
                    break;
                case 8:
                    g.FillRectangle(Brushes.Black, x + m, y + m, m, m);
                    break;
                case 9:
                    g.FillRectangle(Brushes.Black, x + 0, y + 0, m, m);
                    g.FillRectangle(Brushes.Black, x + m, y + m, m, m);
                    break;
                case 10:
                    g.FillRectangle(Brushes.Black, x + m, y + 0, m, dm);
                    break;

                case 11:
                    g.FillRectangle(Brushes.Black, x + 0, y + 0, dm, m);
                    g.FillRectangle(Brushes.Black, x + m, y + m, m, m);
                    break;
                case 12:
                    g.FillRectangle(Brushes.Black, x + 0, y + m, dm, m);
                    break;
                case 13:
                    g.FillRectangle(Brushes.Black, x + 0, y + 0, m, m);
                    g.FillRectangle(Brushes.Black, x + 0, y + m, dm, m);
                    break;
                case 14:
                    g.FillRectangle(Brushes.Black, x + m, y + 0, m, m);
                    g.FillRectangle(Brushes.Black, x + 0, y + m, dm, m);
                    break;
                case 15:
                    g.FillRectangle(Brushes.Black, x + 0, y + 0, dm, dm);
                    break;
                }
                
                x += dm;
            } else {
            }
        }

        return bmp;
    }
}