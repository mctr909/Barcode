using System;
using System.Drawing;

class QR {
    const string SIZE_TABLE = "D01A01K01G01J01D01V01P01T01I01P02L02L02N01J04T02R02T01P04L04J04L02V04R04L04N02T05L06P04R02T06P06P05X02R08N08T05L04V08R08X05N04R11V08P08R04V11T10P09T04P16R12R09X04R16N16R10P06R18X12V10R06X16R17V11V06V19V16T13X06V21V18T14V07T25T21T16V08V25X20T17V08X25V23V17V09R34X23V18X09X30X25V20X10X32X27V21T12X35X29V23V12X37V34V25X12X40X34V26X13X42X35V28X14X45X38V29X15X48X40V31X16X51X43V33X17X54X45V35X18X57X48V37X19X60X51V38X19X63X53V40X20X66X56V43X21X70X59V45X22X74X62V47X24X77X65V49X25X81X68";
    const string QRALNUM = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";

    readonly byte[] EXP_LOG = new byte[513];
    const int LOG_BEGIN = 256;

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

    void generateRS(byte[] table, int size, int len, int blocks) {
        for (int v_x = 1; v_x <= len; v_x++) {
            table[v_x + size] = 0;
        }

        // qr code has first x blocks shorter than lasts
        var v_bs = size / blocks;    // shorter block size
        var v_es = len / blocks;     // ecc block size
        var remain = size % blocks;  // remain bytes
        var v_b2c = blocks - remain; // on block number v_b2c
        var v_ply = new byte[v_es + 2];
        v_ply[1] = 1;

        // pro QR je v_z=0 pro dmx je v_z=1
        for (int v_x = 2, v_z = 0; v_x <= v_es + 1; v_x++, v_z++) {
            int pa, pb, rp;
            v_ply[v_x] = v_ply[v_x - 1];
            for (int v_y = v_x - 1; v_y > 1; v_y--) {
                pb = EXP_LOG[v_z];
                pa = v_ply[v_y];
                rp = rsprod(pa, pb);
                v_ply[v_y] = (byte)(v_ply[v_y - 1] ^ rp);
            }
            pa = v_ply[1];
            pb = EXP_LOG[v_z];
            rp = rsprod(pa, pb);
            v_ply[1] = (byte)rp;
        }

        for (int v_b = 0; v_b <= blocks - 1; v_b++) {
            var vpo = v_b * v_es + 1 + size; // ECC start
            var vdo = v_b * v_bs + 1; // data start
            if (v_b > v_b2c) {
                vdo += v_b - v_b2c; // x longers before
            }
            // generate "nc" checkwords in the array
            var v_z = v_bs;
            if (v_b >= v_b2c) {
                v_z++;
            }
            for (int v_x = 0; v_x < v_z; v_x++) {
                var pa = table[vpo] ^ table[vdo + v_x];
                for (int v_a = v_es, v_y = vpo; v_a > 0; v_a--, v_y++) {
                    var pb = v_ply[v_a];
                    var rp = rsprod(pa, pb);
                    if (v_a == 1) {
                        table[v_y] = (byte)rp;
                    } else {
                        table[v_y] = (byte)(table[v_y + 1] ^ rp);
                    }
                }
            }
        }
    }

    void putbits(byte[] output, int input, int len, ref int offsetBits) {
        if (len > 56) {
            return;
        }

        var arr = new byte[7];
        var dw = (double)input;
        if (len < 56) {
            dw *= (long)1 << (56 - len);
        }
        for (int i = 0; i < 6 && dw > 0; i++) {
            var w = (long)dw >> 48;
            arr[i] = (byte)(w % 256);
            dw -= w << 48;
            dw *= 256;
        }

        var offset_b = offsetBits % 8;
        var offset_i = (offsetBits / 8) + 1;
        for (int i = 0, l = len; 0 < l; i++, l -= 8) {
            int w;
            if (i < arr.Length) {
                w = arr[i];
            } else {
                w = 0;
            }
            if (l < 8) {
                w &= 256 - (1 << (8 - l));
            }
            if (offset_b > 0) {
                w <<= 8 - offset_b;
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
    }

    int putbits(byte[] output, byte[] input, int len, int offsetBits = 0) {
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

    int countbits(int value) {
        int v, n;
        for (v = 1, n = 0; v <= value; v <<= 1, n++) ;
        return n;
    }

    // padding 0xEC,0x11,0xEC,0x11...
    // TYPE_INFO_MASK_PATTERN = 0x5412
    // TYPE_INFO_POLY = 0x537  [(ecLevel << 3) | maskPattern] : 5 + 10 = 15 bitu
    // VERSION_INFO_POLY = 0x1f25 : 5 + 12 = 17 bitu
    void bchCalc(ref int data, int poly) {
        var b = countbits(poly) - 1;
        if (data == 0) {
            //data = poly
            return;
        }
        var x = data << b;
        var rv = x;
        while (true) {
            var n = countbits(rv);
            if (n <= b) {
                break;
            }
            rv ^= poly << (n - b - 1);
        }
        data = x + rv;
    }

    void setParams(int pcap, int ecl, int[] rv, int[] ecx_poc) {
        int i, j;
        int siz = 0, totby = 0;
        int syncs = 0, ccsiz = 0, ccblks = 0, ver = 0;
        //Dim rv(15) as Integer
        // 1:version, 2:size, 3:ccs, 4:ccb,
        // 5:totby, 6-12:syncs(7), 13-15:versinfo(3)
        //ecl:M=0,L=1,H=2,Q=3
        if (ecl < 0 || ecl > 3) return;
        for (i = 1; i < rv.Length; i++) {
            rv[i] = 0;
        }
        j = (pcap + 18 * ecx_poc[1] + 17 * ecx_poc[2] + 20 * ecx_poc[3] + 7) / 8;
        if (ecl == 0 && j > 2334 ||
            ecl == 1 && j > 2956 ||
            ecl == 2 && j > 1276 ||
            ecl == 3 && j > 1666
        ) {
            return;
        }
        j = (pcap + 14 * ecx_poc[1] + 13 * ecx_poc[2] + 12 * ecx_poc[3] + 7) / 8;
        for (ver = 1; ver <= 40; ver++) {
            if (ver == 10) {
                j = (pcap + 16 * ecx_poc[1] + 15 * ecx_poc[2] + 20 * ecx_poc[3] + 7) / 8;
            }
            if (ver == 27) {
                j = (pcap + 18 * ecx_poc[1] + 17 * ecx_poc[2] + 20 * ecx_poc[3] + 7) / 8;
            }
            siz = 4 * ver + 17;
            i = (ver - 1) * 12 + ecl * 3;
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
                totby = siz - 1;
                totby = ((totby * totby) / 8) - (3 * syncs) - 24;
                if (ver > 6) totby -= 4;
                if (syncs == 1) totby--;
            }
            //MsgBox "ver:" & ver & " tot: " & totby & " dat:" & (totby - ccsiz * ccblks) & " need:" & j
            if (totby - ccsiz * ccblks >= j) break;
        }
        if (ver > 1) {
            syncs = (ver / 7) + 2;
            rv[6] = 6;
            rv[5 + syncs] = siz - 7;
            if (syncs > 2) {
                i = (int)((siz - 13) / 2 / (syncs - 1) + 0.7) * 2;
                rv[7] = rv[5 + syncs] - i * (syncs - 2);
                if (syncs > 3) {
                    for (j = 3; j < syncs; j++) {
                        rv[5 + j] = rv[4 + j] + i;
                    }
                }
            }
        }
        rv[1] = ver;
        rv[2] = siz;
        rv[3] = ccsiz;
        rv[4] = ccblks;
        rv[5] = totby;
        if (ver >= 7) {
            i = ver;
            bchCalc(ref i, 0x1F25);
            rv[13] = (i >> 16) & 0xFF;
            rv[14] = (i >> 8) & 0xFF;
            rv[15] = i & 0xFF;
        }
    }

    bool qrbit(byte[][] parr, int psiz, int prow, int pcol, int pbit) {
        var r = prow;
        var c = pcol;
        var ix = r * 24 + c / 8; // 24 bytes per row
        if (ix >= parr[0].Length || ix < 0) {
            return false;
        }
        c = 1 << (c % 8);
        var ret = false;
        var va = parr[0][ix];
        if (psiz > 0) {
            if ((va & c) == 0) {
                if (pbit != 0) {
                    parr[1][ix] |= (byte)c;
                }
                ret = true;
            } else {
                ret = false;
            }
        } else {
            ret = true;
            parr[1][ix] &= (byte)(255 - c); // reset bit for psiz <= 0
            if (pbit > 0) {
                parr[1][ix] |= (byte)c;
            }
            if (psiz < 0) {
                parr[0][ix] |= (byte)c; // mask for psiz < 0
            }
        }
        return ret;
    }

    void mask(byte[][] parr, object pb, int pbits, int pr, int pc) {
        // max 8 bites wide
        bool x;
        if (pbits > 8 || pbits < 1) {
            return;
        }
        var r = pr;
        var c = pc;
        if (pb is byte || pb is int || pb is long || pb is double) { // byte,integer,long, double
            var w = (int)pb;
            var i = 1 << (pbits - 1);
            while (i > 0) {
                x = qrbit(parr, -1, r, c, w & i);
                c++;
                i >>= 1;
            }
        } else if (pb is byte[]) {
            var arr = (byte[])pb;
            for (var j = 0; j < arr.Length; j++) {
                var w = (int)arr[j];
                var i = 1 << (pbits - 1);
                c = pc;
                while (i > 0) {
                    x = qrbit(parr, -1, r, c, w & i);
                    c++;
                    i >>= 1;
                }
                r++;
            }
        }
    }

    void fill(byte[][] parr, int psiz, byte[] pb, int pblocks, int pdlen, int ptlen) {
        // vyplni pole parr (psiz x 24 bytes) z pole pb pdlen = pocet dbytes, pblocks = bloku, ptlen celkem
        // podle logiky qr_kodu - s prokladem

        // qr code has first x blocks shorter than lasts but datamatrix has first longer and shorter last
        var vds = pdlen / pblocks;              // shorter data block size
        var ves = (ptlen - pdlen) / pblocks;    // ecc block size
        var vdnlen = vds * pblocks;             // potud jsou databloky stejne velike
        var vsb = pblocks - (pdlen % pblocks);// mensich databloku je ?

        var c = psiz - 1;
        var r = c;    // start position on right lower corner
        var smer = 0; // nahoru :  3 <- 2 10  dolu: 1 <- 0  32
                      //           1 <- 0 10        3 <- 2  32
        var vb = 1;
        var w = pb[1];
        var vx = 0;
        while (c >= 0 && vb <= ptlen) {
            if (qrbit(parr, psiz, r, c, w & 128)) {
                vx++;
                if (vx == 8) {
                    qrfnb(pb, ref w, ref vb, vds, ves, vsb, pdlen, ptlen, vdnlen, pblocks); // first byte
                    vx = 0;
                } else {
                    w = (byte)((w << 1) & 0xFF);
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
                    if (c == 6 && psiz >= 21) {
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
                if (r == (psiz - 1)) { // dolu uz to nepude
                    c--;
                    if (c == 6 && psiz >= 21) {
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

    void qrfnb(byte[] pb, ref byte w, ref int vb,
        int vds, int ves, int vsb,
        int pdlen, int ptlen, int vdnlen,
        int pblocks
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
        if (vb < pdlen) { // Datovy byte
            var wa = vb;
            if (vb >= vdnlen) {
                wa += vsb;
            }
            var wb = wa % pblocks;
            wa /= pblocks;
            if (wb > vsb) {
                wa += wb - vsb;
            }
            //If vb >= vdnlen Then MsgBox "D:" & (1 + vds * wb + wa)
            w = pb[1 + vds * wb + wa];
        } else if (vb < ptlen) { // ecc byte
            var wa = vb - pdlen;   // kolikaty ecc 0..x
            var wb = wa % pblocks; // z bloku
            wa /= pblocks;         // kolikaty
                                   //MsgBox "E:" & (1 + pdlen + ves * wb + wa)
            w = pb[1 + pdlen + ves * wb + wa];
        }
        vb++;
    }

    // Black If 0: (c+r) mod 2 = 0    4: ((r div 2) + (c div 3)) mod 2 = 0
    //          1: r mod 2 = 0        5: (c*r) mod 2 + (c*r) mod 3 = 0
    //          2: c mod 3 = 0        6: ((c*r) mod 2 + (c*r) mod 3) mod 2 = 0
    //          3: (c+r) mod 3 = 0    7: ((c+r) mod 2 + (c*r) mod 3) mod 2 = 0
    int xormask(byte[][] parr, int siz, int pmod, bool final) {
        int c, r, i;
        var warr = new byte[siz * 24];
        for (r = 0; r < siz; r++) {
            var m = 1;
            var ix = 24 * r;
            warr[ix] = parr[1][ix];
            for (c = 0; c < siz; c++) {
                if ((parr[0][ix] & m) == 0) { // nemaskovany
                    switch (pmod) {
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
                        parr[1][ix] = warr[ix];
                    }
                    ix++;
                    warr[ix] = parr[1][ix];
                } else {
                    m <<= 1;
                }
            }
            if (m != 128 && final) {
                parr[1][ix] = warr[ix];
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
        var bl = 0;
        int[,] cols = new int[2, siz];
        for (r = 0; r < siz; r++) {
            var m = 1;
            var ix = 24 * r;
            var rp = 0;
            var rc = 0;
            for (c = 0; c < siz; c++) {
                rp = (rp & 0x3FF) << 1; // only last 12 bits
                cols[1, c] = (cols[1, c] & 0x3FF) << 1;
                if ((warr[ix] & m) != 0) {
                    if (rc < 0) { // in row x whites
                        if (rc <= -5) {
                            score -= rc + 2; //: s(0) = s(0) - 2 - rc
                        }
                        rc = 0;
                    }
                    rc++; // one more black
                    if (cols[0, c] < 0) { // color changed
                        if (cols[0, c] <= -5) {
                            score -= cols[0, c] + 2; //: s(1) = s(1) - 2 - cols(0,c)
                        }
                        cols[0, c] = 0;
                    }
                    cols[0, c]++; // one more black
                    rp |= 1;
                    cols[1, c] |= 1;
                    bl++; // balck modules count
                } else {
                    if (rc > 0) { // in row x black
                        if (rc >= 5) {
                            score += rc - 2; //: s(0) = s(0) - 2 + rc
                        }
                        rc = 0;
                    }
                    rc--; // one more white
                    if (cols[0, c] > 0) { // color changed
                        if (cols[0, c] >= 5) {
                            score += cols[0, c] - 2; //: s(1) = s(1) - 2 + cols(0,c)
                        }
                        cols[0, c] = 0;
                    }
                    cols[0, c]--; // one more white
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
                        score += 3; //: s(2) = s(2) + 3
                                    // b) penalty na 2x2 block same color
                    }
                }
                if (c >= 10 && (rp == 0x5D || rp == 0x5D0)) { // penalty pattern c in row
                    score += 40; //: s(3) = s(3) + 40
                }
                if (r >= 10 && (cols[1, c] == 0x5D || cols[1, c] == 0x5D0)) { // penalty pattern c in column
                    score += 40; //: s(3) = s(3) + 40
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
                score -= rc + 2; //: s(0) = s(0) - 2 - rc
            }
            if (rc >= 5) {
                score += rc - 2; //: s(0) = s(0) - 2 + rc
            }
        }
        for (c = 0; c < siz; c++) { // after last row count column blocks
            if (cols[0, c] <= -5) {
                score -= cols[0, c] + 2; //: s(1) = s(1) - 2 - cols(0,c)
            }
            if (cols[0, c] >= 5) {
                score += cols[0, c] - 2; //: s(1) = s(1) - 2 + cols(0,c)
            }
        }
        bl = (Math.Abs((bl * 100) / (siz * siz) - 50) / 5) * 10;
        //MsgBox "mask:" + pmod + " " + s(0) + "+" + s(1) + "+" + s(2) + "+" + s(3) + "+" + bl
        return score + bl;
    }

    string gen(string ptext, string poptions) {
        var ecx_cnt = new int[4];
        var ecx_pos = new int[4];
        var ecx_poc = new int[4];
        var eb = new int[20, 5];

        int i = 0, j = 0, k = 0, m = 0;
        string ascimatrix = "", err = "";

        int ecl, r, c, mask, utf8, ebcnt;
        int ch = 0, s = 0, siz = 0;

        //Dim qrpos As Integer
        var qrp = new int[16]; // 1:version,2:size,3:ccs,4:ccb,5:totby,6-12:syncs(7),13-15:versinfo(3)

        var mode = "M";
        //i = InStr(poptions, "mode=")
        if (i > 0) {
            mode = poptions.Substring(i + 5, 1);
        }
        //M=0,L=1,H=2,Q=3
        ecl = "MLHQ".IndexOf(mode) - 1;
        if (ecl < 0) {
            mode = "M";
            ecl = 0;
        }
        if (ptext == "") {
            err = "Not data";
            return "";
        }
        for (i = 0; i < 4; i++) {
            ecx_pos[i] = 0;
            ecx_cnt[i] = 0;
            ecx_poc[i] = 0;
        }
        ebcnt = 1;
        utf8 = 0;
        for (i = 0; i <= ptext.Length; i++) {
            if (i >= ptext.Length) {
                k = -5;
            } else {
                k = ptext.Substring(i, 1).ToCharArray()[0];
                if (k >= 0x1FFFFF) { // FFFF - 1FFFFFFF
                    m = 4;
                    k = -1;
                } else if (k >= 0x7FF) { // 7FF-FFFF 3 bytes
                    m = 3;
                    k = -1;
                } else if (k >= 128) {
                    m = 2;
                    k = -1;
                } else {
                    m = 1;
                    k = QRALNUM.IndexOf(ptext.Substring(i, 1)) - 1;
                }
            }
            if (k < 0) { // bude byte nebo konec
                if (ecx_cnt[1] >= 9 || (k == -5 && ecx_cnt[1] == ecx_cnt[3])) { // Az dosud bylo mozno pouzitelne numeric
                    if ((ecx_cnt[2] - ecx_cnt[1]) >= 8 || (ecx_cnt[3] == ecx_cnt[2])) { // pred num je i pouzitelny alnum
                        if (ecx_cnt[3] > ecx_cnt[2]) { // Jeste pred alnum bylo byte
                            eb[ebcnt, 1] = 3;          // Typ byte
                            eb[ebcnt, 2] = ecx_pos[3]; // pozice
                            eb[ebcnt, 3] = ecx_cnt[3] - ecx_cnt[2]; // delka
                            ebcnt++;
                            ecx_poc[3]++;
                        }
                        eb[ebcnt, 1] = 2;         // Typ alnum
                        eb[ebcnt, 2] = ecx_pos[2];
                        eb[ebcnt, 3] = ecx_cnt[2] - ecx_cnt[1]; // delka
                        ebcnt++;
                        ecx_poc[2]++;
                        ecx_cnt[2] = 0;
                    } else if (ecx_cnt[3] > ecx_cnt[1]) { // byly bytes pred numeric
                        eb[ebcnt, 1] = 3;          // Typ byte
                        eb[ebcnt, 2] = ecx_pos[3]; // pozice
                        eb[ebcnt, 3] = ecx_cnt[3] - ecx_cnt[1]; // delka
                        ebcnt++;
                        ecx_poc[3]++;
                    }
                } else if ((ecx_cnt[2] >= 8) || (k == -5 && ecx_cnt[2] == ecx_cnt[3])) { // Az dosud bylo mozno pouzitelne alnum
                    if (ecx_cnt[3] > ecx_cnt[2]) { // Jeste pred alnum bylo byte
                        eb[ebcnt, 1] = 3;          // Typ byte
                        eb[ebcnt, 2] = ecx_pos[3]; // pozice
                        eb[ebcnt, 3] = ecx_cnt[3] - ecx_cnt[2]; // delka
                        ebcnt++;
                        ecx_poc[3]++;
                    }
                    eb[ebcnt, 1] = 2;          // Typ alnum
                    eb[ebcnt, 2] = ecx_pos[2];
                    eb[ebcnt, 3] = ecx_cnt[2]; // delka
                    ebcnt++;
                    ecx_poc[2]++;
                    ecx_cnt[3] = 0;
                    ecx_cnt[2] = 0; // vse zpracovano
                } else if (k == -5 && ecx_cnt[3] > 0) { // konec ale mam co ulozit
                    eb[ebcnt, 1] = 3;          // Typ byte
                    eb[ebcnt, 2] = ecx_pos[3]; // pozice
                    eb[ebcnt, 3] = ecx_cnt[3]; // delka
                    ebcnt++;
                    ecx_poc[3]++;
                }
            }
            if (k == -5) {
                break;
            }
            if (k >= 0) { // Muzeme alnum
                if (k >= 10 && ecx_cnt[1] >= 12) { // Az dosud bylo mozno num
                    if ((ecx_cnt[2] - ecx_cnt[1]) >= 8 || (ecx_cnt[3] == ecx_cnt[2])) { // Je tam i alnum ktery stoji za to
                        if (ecx_cnt[3] > ecx_cnt[2]) { // Jeste pred alnum bylo byte
                            eb[ebcnt, 1] = 3;          // Typ byte
                            eb[ebcnt, 2] = ecx_pos[3]; // pozice
                            eb[ebcnt, 3] = ecx_cnt[3] - ecx_cnt[2]; // delka
                            ebcnt++;
                            ecx_poc[3]++;
                        }
                        eb[ebcnt, 1] = 2;          // Typ alnum
                        eb[ebcnt, 2] = ecx_pos[2];
                        eb[ebcnt, 3] = ecx_cnt[2] - ecx_cnt[1]; // delka
                        ebcnt++;
                        ecx_poc[2]++;
                        ecx_cnt[2] = 0; // vse zpracovano
                    } else if (ecx_cnt[3] > ecx_cnt[1]) { // Pred Num je byte
                        eb[ebcnt, 1] = 3;          // Typ byte
                        eb[ebcnt, 2] = ecx_pos[3]; // pozice
                        eb[ebcnt, 3] = ecx_cnt[3] - ecx_cnt[1]; // delka
                        ebcnt++;
                        ecx_poc[3]++;
                    }
                    eb[ebcnt, 1] = 1;          // Typ numerix
                    eb[ebcnt, 2] = ecx_pos[1];
                    eb[ebcnt, 3] = ecx_cnt[1]; // delka
                    ebcnt++;
                    ecx_poc[1] = ecx_poc[1] + 1;
                    ecx_cnt[1] = 0;
                    ecx_cnt[2] = 0;
                    ecx_cnt[3] = 0; // vse zpracovano
                }
                if (ecx_cnt[2] == 0) {
                    ecx_pos[2] = i;
                }
                ecx_cnt[2]++;
            } else { // mozno alnum
                ecx_cnt[2] = 0;
            }
            if (k >= 0 && k < 10) { // muze byt numeric
                if (ecx_cnt[1] == 0) {
                    ecx_pos[1] = i;
                }
                ecx_cnt[1]++;
            } else {
                ecx_cnt[1] = 0;
            }
            if (ecx_cnt[3] == 0) {
                ecx_pos[3] = i;
            }
            ecx_cnt[3] += m;
            utf8 += m;
            if (ebcnt >= 16) { // Uz by se mi tri dalsi bloky stejne nevesli
                ecx_cnt[1] = 0;
                ecx_cnt[2] = 0;
            }
            //MsgBox "Znak:" & Mid(ptext,i,1) & "(" & k & ") ebn=" & ecx_pos(1) & "." & ecx_cnt(1) & " eba=" & ecx_pos(2) & "." & ecx_cnt(2) & " ebb=" & ecx_pos(3) & "." & ecx_cnt(3)
        }
        ebcnt--;

        c = 0;
        for (i = 1; i <= ebcnt; i++) {
            switch (eb[i, 1]) {
            case 1:
                eb[i, 4] = (eb[i, 3] / 3) * 10 + (eb[i, 3] % 3) * 3 + ((eb[i, 3] % 3) > 0 ? 1 : 0); break;
            case 2:
                eb[i, 4] = (eb[i, 3] / 2) * 11 + (eb[i, 3] % 2) * 6; break;
            case 3:
                eb[i, 4] = eb[i, 3] * 8; break;
            }
            c += eb[i, 4];
        }
        // UTF-8 is default not need ECI value - zxing cannot recognize
        //Call qr_params(i * 8 + utf8,mode,qrp)
        setParams(c, ecl, qrp, ecx_poc);
        if (qrp[1] <= 0) {
            err = "Too long";
            return "";
        }
        siz = qrp[2];
        //MsgBox "ver:" & qrp(1) & mode & " size " & siz & " ecc:" & qrp(3) & "x" & qrp(4) & " d:" & (qrp(5) - qrp(3) * qrp(4))

        var encoded1 = new byte[qrp[5] + 2];
        // byte mode (ASCII) all max 3200 bytes
        // mode indicator (1=num,2=AlNum,4=Byte,8=kanji,ECI=7)
        //      mode: Byte Alhanum  Numeric  Kanji
        // ver 1..9 :  8      9       11       8
        //   10..26 : 16     11       12      10
        //   27..40 : 16     13       14      12
        // UTF-8 is default not need ECI value - zxing cannot recognize
        // if utf8 > 0 Then
        //   k = &H700 + 26 ' UTF - 8 = 26; Win1250 = 21; 8859 - 2 = 4 viz http://strokescribe.com/en/ECI.html
        //   bb_putbits(encoded1,encix1,k,12)
        // End If
        int encix1 = 0;
        for (i = 1; i <= ebcnt; i++) {
            switch (eb[i, 1]) {
            case 1:
                c = qrp[1] < 10 ? 10 : (qrp[1] < 27 ? 12 : 14);
                k = (1 << c) + eb[i, 3];
                break;
            case 2:
                c = qrp[1] < 10 ? 9 : (qrp[1] < 27 ? 11 : 13);
                k = (2 << c) + eb[i, 3];
                break;
            case 3:
                c = qrp[1] < 10 ? 8 : 16;
                k = (4 << c) + eb[i, 3];
                break;
            }
            putbits(encoded1, k, c + 4, ref encix1);
            j = 0;
            m = eb[i, 2];
            r = 0;
            while (j < eb[i, 3]) {
                k = ptext.Substring(m, 1).ToCharArray()[0];
                m++;
                if (eb[i, 1] == 1) {
                    r = (r * 10) + ((k - 0x30) % 10);
                    if ((j % 3) == 2) {
                        putbits(encoded1, r, 10, ref encix1);
                        r = 0;
                    }
                    j++;
                } else if (eb[i, 1] == 2) {
                    r = (r * 45) + (QRALNUM.IndexOf((char)(k - 1)) % 45);
                    if ((j % 2) == 1) {
                        putbits(encoded1, r, 11, ref encix1);
                        r = 0;
                    }
                    j++;
                } else {
                    if (k > 0x1FFFFF) { // FFFF - 1FFFFFFF
                        ch = 0xF0 + (k / 0x40000) % 8;
                        putbits(encoded1, ch, 8, ref encix1);
                        ch = 128 + (k / 0x1000) % 64;
                        putbits(encoded1, ch, 8, ref encix1);
                        ch = 128 + (k / 64) % 64;
                        putbits(encoded1, ch, 8, ref encix1);
                        ch = 128 + k % 64;
                        putbits(encoded1, ch, 8, ref encix1);
                        j += 4;
                    } else if (k > 0x7FF) { // 7FF-FFFF 3 bytes
                        ch = 0xE0 + (k / 0x1000) % 16;
                        putbits(encoded1, ch, 8, ref encix1);
                        ch = 128 + (k / 64) % 64;
                        putbits(encoded1, ch, 8, ref encix1);
                        ch = 128 + k % 64;
                        putbits(encoded1, ch, 8, ref encix1);
                        j += 3;
                    } else if (k > 0x7F) { // 2 bytes
                        ch = 0xC0 + (k / 64) % 32;
                        putbits(encoded1, ch, 8, ref encix1);
                        ch = 128 + k % 64;
                        putbits(encoded1, ch, 8, ref encix1);
                        j += 2;
                    } else {
                        ch = k % 256;
                        putbits(encoded1, ch, 8, ref encix1);
                        j++;
                    }
                }
            }
            switch (eb[i, 1]) {
            case 1:
                if ((j % 3) == 1) {
                    putbits(encoded1, r, 4, ref encix1);
                } else if ((j % 3) == 2) {
                    putbits(encoded1, r, 7, ref encix1);
                }
                break;
            case 2:
                if ((j % 2) == 1) {
                    putbits(encoded1, r, 6, ref encix1);
                }
                break;
            }
            //MsgBox "blk[" & i & "] t:" & eb(i,1) & "from " & eb(i,2) & " to " & eb(i,3) + eb(i,2) & " bits=" & encix1
        }

        putbits(encoded1, 0, 4, ref encix1); // end of chain
        if ((encix1 % 8) != 0) { // round to byte
            putbits(encoded1, 0, 8 - (encix1 % 8), ref encix1);
        }
        // padding
        i = (qrp[5] - qrp[3] * qrp[4]) * 8;
        if (encix1 > i) {
            err = "Encode length error";
            return "";
        }
        // padding 0xEC,0x11,0xEC,0x11...
        while (encix1 < i) {
            putbits(encoded1, -5103, 16, ref encix1);
        }
        // doplnime ECC
        i = qrp[3] * qrp[4]; //ppoly, pmemptr , psize , plen , pblocks
        generateRS(encoded1, qrp[5] - i, i, qrp[4]);
        //Call arr2hexstr(encoded1)
        encix1 = qrp[5];

        // Pole pro vystup
        var qrarr = new byte[2][]; // 24 bytes per row
        qrarr[0] = new byte[qrp[2] * 24 + 24];
        qrarr[1] = new byte[qrp[2] * 24 + 24];
        qrarr[0][0] = 0;
        var qrsync1 = new byte[8];
        var qrsync2 = new byte[5];
        putbits(qrsync1, new byte[] { 0xFE, 0x82, 0xBA, 0xBA, 0xBA, 0x82, 0xFE, 0 }, 64);
        this.mask(qrarr, qrsync1, 8, 0, 0); // sync UL
        this.mask(qrarr, 0, 8, 8, 0); // fmtinfo UL under - bity 14..9 SYNC 8
        this.mask(qrarr, qrsync1, 8, 0, siz - 7); // sync UR ( o bit vlevo )
        this.mask(qrarr, 0, 8, 8, siz - 8); // fmtinfo UR - bity 7..0
        this.mask(qrarr, qrsync1, 8, siz - 7, 0); // sync DL (zasahuje i do quiet zony)
        this.mask(qrarr, 0, 8, siz - 8, 0); // blank nad DL

        bool x;
        for (i = 0; i <= 6; i++) {
            x = qrbit(qrarr, -1, i, 8, 0);           // svisle fmtinfo UL - bity 0..5 SYNC 6,7
            x = qrbit(qrarr, -1, i, siz - 8, 0);     // svisly blank pred UR
            x = qrbit(qrarr, -1, siz - 1 - i, 8, 0); // svisle fmtinfo DL - bity 14..8
        }
        x = qrbit(qrarr, -1, 7, 8, 0);       // svisle fmtinfo UL - bity 0..5 SYNC 6,7
        x = qrbit(qrarr, -1, 7, siz - 8, 0); // svisly blank pred UR
        x = qrbit(qrarr, -1, 8, 8, 0);       // svisle fmtinfo UL - bity 0..5 SYNC 6,7
        x = qrbit(qrarr, -1, siz - 8, 8, 1); // black dot DL
        if (qrp[13] != 0 || qrp[14] != 0) { // versioninfo
            // UR ver 0 1 2;3 4 5;...;15 16 17
            // LL ver 0 3 6 9 12 15;1 4 7 10 13 16; 2 5 8 11 14 17
            k = 65536 * qrp[13] + 256 * qrp[14] + 1 * qrp[15];
            c = 0; r = 0;
            for (i = 0; i <= 17; i++) {
                ch = k % 2;
                x = qrbit(qrarr, -1, r, siz - 11 + c, ch); // UR ver
                x = qrbit(qrarr, -1, siz - 11 + c, r, ch); // DL ver
                c++;
                if (c > 2) {
                    c = 0;
                    r++;
                }
                k >>= 1;
            }
        }
        c = 1;
        for (i = 8; i <= siz - 9; i++) { // sync lines
            x = qrbit(qrarr, -1, i, 6, c); // vertical on column 6
            x = qrbit(qrarr, -1, 6, i, c); // horizontal on row 6
            c = (c + 1) % 2;
        }

        // other syncs
        putbits(qrsync2, new byte[] { 0x1F, 0x11, 0x15, 0x11, 0x1F }, 40);
        ch = 6;
        while (ch > 0 && qrp[6 + ch] == 0) {
            ch--;
        }
        if (ch > 0) {
            for (c = 0; c <= ch; c++) {
                for (r = 0; r <= ch; r++) {
                    // corners
                    if ((c != 0 || r != 0) && (c != ch || r != 0) && (c != 0 || r != ch)) {
                        this.mask(qrarr, qrsync2, 5, qrp[r + 6] - 2, qrp[c + 6] - 2);
                    }
                }
            }
        }

        // qr_fill(parr as Variant, psiz%, pb as Variant, pblocks%, pdlen%, ptlen%)
        // vyplni pole parr (psiz x 24 bytes) z pole pb pdlen = pocet dbytes, pblocks = bloku, ptlen celkem
        fill(qrarr, siz, encoded1, qrp[4], qrp[5] - qrp[3] * qrp[4], qrp[5]);
        mask = 8; // auto
        i = poptions.IndexOf("mask=");
        if (i >= 0) {
            mask = poptions.Substring(i + 5, 1).ToCharArray()[0];
        }

        if (mask < 0 || mask > 7) {
            j = -1;
            for (mask = 0; mask <= 7; mask++) {
                addmm(qrarr, ecl, mask, siz);
                i = xormask(qrarr, siz, mask, false);
                //MsgBox "score mask " & mask & " is " & i
                if (i < j || j == -1) {
                    j = i;
                    s = mask;
                }
            }
            mask = s;
            //MsgBox "best is " & mask & " with score " & j
        }
        addmm(qrarr, ecl, mask, siz);

        i = xormask(qrarr, siz, mask, true);
        ascimatrix = "";
        for (r = 0; r <= siz; r += 2) {
            s = 0;
            for (c = 0; c <= siz; c += 2) {
                if ((c % 8) == 0) {
                    ch = qrarr[1][s + 24 * r];
                    if (r < siz) {
                        i = qrarr[1][s + 24 * (r + 1)];
                    } else {
                        i = 0;
                    }
                    s++;
                }
                ascimatrix += ((char)(97 + (ch % 4) + 4 * (i % 4))).ToString();
                ch = (ch / 4);
                i = (i / 4);
            }
            ascimatrix += "\n";
        }
        return ascimatrix;
    }

    void addmm(byte[][] qrarr, int ecl, int mask, int siz) {
        var k = ecl * 8 + mask;
        // poly: 101 0011 0111
        bchCalc(ref k, 0x537);
        //MsgBox "mask :" & hex(k,3) & " " & hex(k xor &H5412,3)
        k = k ^ 0x5412; // micro xor &H4445
        var r = 0;
        var c = siz - 1;
        bool x;
        for (int i = 0; i <= 14; i++) {
            var ch = k % 2;
            k >>= 1;
            x = qrbit(qrarr, -1, r, 8, ch); // svisle fmtinfo UL - bity 0..5 SYNC 6,7 .... 8..14 dole
            x = qrbit(qrarr, -1, 8, c, ch); // vodorovne odzadu 0..7 ............ 8,SYNC,9..14
            c--;
            r++;
            if (i == 7) {
                c = 7;
                r = siz - 7;
            }
            if (i == 5) {
                r++; // preskoc sync vodorvny
            }
            if (i == 8) {
                c--; // preskoc sync svisly
            }
        }
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