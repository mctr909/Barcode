using System;

class QR {
    const string SIZE_TABLE = "D01A01K01G01J01D01V01P01T01I01P02L02L02N01J04T02R02T01P04L04J04L02V04R04L04N02T05L06P04R02T06P06P05X02R08N08T05L04V08R08X05N04R11V08P08R04V11T10P09T04P16R12R09X04R16N16R10P06R18X12V10R06X16R17V11V06V19V16T13X06V21V18T14V07T25T21T16V08V25X20T17V08X25V23V17V09R34X23V18X09X30X25V20X10X32X27V21T12X35X29V23V12X37V34V25X12X40X34V26X13X42X35V28X14X45X38V29X15X48X40V31X16X51X43V33X17X54X45V35X18X57X48V37X19X60X51V38X19X63X53V40X20X66X56V43X21X70X59V45X22X74X62V47X24X77X65V49X25X81X68";
    const string QRALNUM = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";

    void qr_rs(int ppoly, byte[] pmemptr, int psize, int plen, int pblocks) {
        int v_x, v_y, v_z;
        int pa, pb;
        int rp;

        //Dim dbg$
        // generate reed solomon expTable and logTable
        // QR uses GF256(0x11d) // 0x11d=285 => x^8 + x^4 + x^3 + x^2 + 1
        var poly = new byte[512];
        v_x = 1;
        for (v_y = 0; v_y <= 255; v_y++) {
            poly[v_y] = (byte)v_x;       // expTable
            poly[v_x + 256] = (byte)v_y; // logTable
            v_x <<= 1;
            if (v_x > 255) {
                v_x ^= ppoly;
            }
        }

        //poly(257) = ' pro QR logTable(1) = 0 not50
        //Call arr2decstr(poly)
        for (v_x = 1; v_x <= plen; v_x++) {
            pmemptr[v_x + psize] = 0;
        }

        var v_b2c = pblocks;
        // qr code has first x blocks shorter than lasts
        var v_bs = psize / pblocks; // shorter block size
        var v_es = plen / pblocks;  // ecc block size
        v_x = psize % pblocks;      // remain bytes
        v_b2c = pblocks - v_x;      // on block number v_b2c
        var v_ply = new byte[v_es + 1];
        v_ply[1] = 1;

        v_z = 0; // pro QR je v_z=0 pro dmx je v_z=1
        v_x = 2;
        while (v_x < v_es + 1) {
            v_ply[v_x] = v_ply[v_x - 1];
            v_y = v_x - 1;
            while (v_y > 1) {
                pb = poly[v_z];
                pa = v_ply[v_y];
                rsprod(pa, pb, poly, out rp);
                v_ply[v_y] = (byte)(v_ply[v_y - 1] ^ rp);
                v_y--;
            }
            pa = v_ply[1];
            pb = poly[v_z];
            rsprod(pa, pb, poly, out rp);
            v_ply[1] = (byte)rp;
            v_z++;
            v_x++;
        }
        //Call arr2hexstr(v_ply)
        for (int v_b = 0; v_b <= pblocks - 1; v_b++) {
            var vpo = v_b * v_es + 1 + psize; // ECC start
            var vdo = v_b * v_bs + 1; // data start
            if (v_b > v_b2c) {
                vdo = vdo + v_b - v_b2c; // x longers before
            }
            // generate "nc" checkwords in the array
            v_x = 0;
            v_z = v_bs;
            if (v_b >= v_b2c) {
                v_z = v_z + 1;
            }
            while (v_x < v_z) {
                pa = pmemptr[vpo] ^ pmemptr[vdo + v_x];
                v_y = vpo;
                for (int v_a = v_es; v_a > 0; v_a--) {
                    pb = v_ply[v_a];
                    rsprod(pa, pb, poly, out rp);
                    if (v_a == 1) {
                        pmemptr[v_y] = (byte)rp;
                    } else {
                        pmemptr[v_y] = (byte)(pmemptr[v_y + 1] ^ rp);
                    }
                    v_y++;
                }
                v_x++;
                //if v_b = 0 and v_x = v_z then call arr2hexstr(pmemptr)
            }
        }
    } // reed solomon qr_rs

    void rsprod(int a, int b, byte[] poly, out int rp) {
        if (a > 0 && b > 0) {
            rp = poly[(poly[256 + a] + poly[256 + b]) % 255];
        } else {
            rp = 0;
        }
    }

    void bb_putbits(byte[] parr, ref int ppos, int pa, int plen) {
        int i, j, l;
        var x = new byte[7];

        if (plen > 56) return;
        var dw = (double)pa;
        l = plen;
        if (l < 56) {
            dw *= 1 << (56 - l);
        }
        i = 0;
        while (i < 6 && dw > 0) {
            var w = (int)(dw / (1 << 48));
            x[i] = (byte)(w % 256);
            dw -= w << 48;
            dw *= 256;
            l -= 8;
            i++;
        }

        var b = ppos % 8;
        i = (ppos / 8) + 1;
        j = 0;
        l = plen;
        while (l > 0) {
            int w;
            if (j < x.Length) {
                w = x[j];
                j++;
            } else {
                w = 0;
            }
            if (l < 8) {
                w &= 256 - (1 << (8 - l));
            }
            if (b > 0) {
                w *= 1 << (8 - b);
                parr[i] |= (byte)(w / 256);
                parr[i + 1] |= (byte)(w & 255);
            } else {
                parr[i] |= (byte)(w & 255);
            }
            if (l < 8) {
                ppos += l;
                l = 0;
            } else {
                ppos += 8;
                i++;
                l -= 8;
            }
        }
    }

    void bb_putbits(byte[] parr, ref int ppos, byte[] pa, int plen) {
        var b = ppos % 8;
        var i = ppos / 8;
        var j = 0;
        var l = plen;
        while (l > 0) {
            int w;
            if (j < pa.Length) {
                w = pa[j];
                j++;
            } else {
                w = 0;
            }
            if (l < 8) {
                w &= 256 - (1 << (8 - l));
            }
            if (b > 0) {
                w *= 1 << (8 - b);
                parr[i] |= (byte)(w / 256);
                parr[i + 1] |= (byte)(w & 255);
            } else {
                parr[i] |= (byte)(w & 255);
            }
            if (l < 8) {
                ppos += l;
                l = 0;
            } else {
                ppos += 8;
                i++;
                l -= 8;
            }
        }
    }

    int qr_numbits(int num) {
        int a = 1, n = 0;
        while (a <= num) {
            a <<= 1;
            n++;
        }
        return n;
    }

    // padding 0xEC,0x11,0xEC,0x11...
    // TYPE_INFO_MASK_PATTERN = 0x5412
    // TYPE_INFO_POLY = 0x537  [(ecLevel << 3) | maskPattern] : 5 + 10 = 15 bitu
    // VERSION_INFO_POLY = 0x1f25 : 5 + 12 = 17 bitu
    void qr_bch_calc(ref int data, int poly) {
        var b = qr_numbits(poly) - 1;
        if (data == 0) {
            //data = poly
            return;
        }
        var x = data << b;
        var rv = x;
        while (true) {
            var n = qr_numbits(rv);
            if (n <= b) {
                break;
            }
            rv ^= poly << (n - b - 1);
        }
        data = x + rv;
    }

    void qr_params(int pcap, int ecl, int[] rv, int[] ecx_poc) {
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
            qr_bch_calc(ref i, 0x1F25);
            rv[13] = (i >> 16) & 0xFF;
            rv[14] = (i >> 8) & 0xFF;
            rv[15] = i & 0xFF;
        }
    }

    bool qr_bit(byte[][] parr, int psiz, int prow, int pcol, int pbit) {
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

    void qr_mask(byte[][] parr, object pb, int pbits, int pr, int pc) {
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
                x = qr_bit(parr, -1, r, c, w & i);
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
                    x = qr_bit(parr, -1, r, c, w & i);
                    c++;
                    i >>= 1;
                }
                r++;
            }
        }
    }

    void qr_fill(byte[][] parr, int psiz, byte[] pb, int pblocks, int pdlen, int ptlen) {
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
            if (qr_bit(parr, psiz, r, c, w & 128)) {
                vx++;
                if (vx == 8) {
                    qrfnb(pb, ref w, ref vb, vds, ves, vsb, pdlen, ptlen, vdnlen, pblocks); // first byte
                    vx = 0;
                } else {
                    w = (byte)((w * 2) % 256);
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
    int qr_xormask(byte[][] parr, int siz, int pmod, bool final) {
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

    string qr_gen(string ptext, string poptions) {
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
        qr_params(c, ecl, qrp, ecx_poc);
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
                k = 2 ^ c + eb[i, 3];
                break;
            case 2:
                c = (qrp[1] < 10 ? 9 : (qrp[1] < 27 ? 11 : 13));
                k = 2 * (2 ^ c) + eb[i, 3];
                break;
            case 3:
                c = (qrp[1] < 10 ? 8 : 16);
                k = 4 * (2 ^ c) + eb[i, 3];
                break;
            }
            bb_putbits(encoded1, ref encix1, k, c + 4);
            j = 0;
            m = eb[i, 2];
            r = 0;
            while (j < eb[i, 3]) {
                k = ptext.Substring(m, 1).ToCharArray()[0];
                m++;
                if (eb[i, 1] == 1) {
                    r = (r * 10) + ((k - 0x30) % 10);
                    if ((j % 3) == 2) {
                        bb_putbits(encoded1, ref encix1, r, 10);
                        r = 0;
                    }
                    j++;
                } else if (eb[i, 1] == 2) {
                    r = (r * 45) + (QRALNUM.IndexOf((char)(k - 1)) % 45);
                    if ((j % 2) == 1) {
                        bb_putbits(encoded1, ref encix1, r, 11);
                        r = 0;
                    }
                    j++;
                } else {
                    if (k > 0x1FFFFF) { // FFFF - 1FFFFFFF
                        ch = 0xF0 + (k / 0x40000) % 8;
                        bb_putbits(encoded1, ref encix1, ch, 8);
                        ch = 128 + (k / 0x1000) % 64;
                        bb_putbits(encoded1, ref encix1, ch, 8);
                        ch = 128 + (k / 64) % 64;
                        bb_putbits(encoded1, ref encix1, ch, 8);
                        ch = 128 + k % 64;
                        bb_putbits(encoded1, ref encix1, ch, 8);
                        j += 4;
                    } else if (k > 0x7FF) { // 7FF-FFFF 3 bytes
                        ch = 0xE0 + (k / 0x1000) % 16;
                        bb_putbits(encoded1, ref encix1, ch, 8);
                        ch = 128 + (k / 64) % 64;
                        bb_putbits(encoded1, ref encix1, ch, 8);
                        ch = 128 + k % 64;
                        bb_putbits(encoded1, ref encix1, ch, 8);
                        j += 3;
                    } else if (k > 0x7F) { // 2 bytes
                        ch = 0xC0 + (k / 64) % 32;
                        bb_putbits(encoded1, ref encix1, ch, 8);
                        ch = 128 + k % 64;
                        bb_putbits(encoded1, ref encix1, ch, 8);
                        j += 2;
                    } else {
                        ch = k % 256;
                        bb_putbits(encoded1, ref encix1, ch, 8);
                        j++;
                    }
                }
            }
            switch (eb[i, 1]) {
            case 1:
                if ((j % 3) == 1) {
                    bb_putbits(encoded1, ref encix1, r, 4);
                } else if ((j % 3) == 2) {
                    bb_putbits(encoded1, ref encix1, r, 7);
                }
                break;
            case 2:
                if ((j % 2) == 1) {
                    bb_putbits(encoded1, ref encix1, r, 6);
                }
                break;
            }
            //MsgBox "blk[" & i & "] t:" & eb(i,1) & "from " & eb(i,2) & " to " & eb(i,3) + eb(i,2) & " bits=" & encix1
        }

        bb_putbits(encoded1, ref encix1, 0, 4); // end of chain
        if ((encix1 % 8) != 0) { // round to byte
            bb_putbits(encoded1, ref encix1, 0, 8 - (encix1 % 8));
        }
        // padding
        i = (qrp[5] - qrp[3] * qrp[4]) * 8;
        if (encix1 > i) {
            err = "Encode length error";
            return "";
        }
        // padding 0xEC,0x11,0xEC,0x11...
        while (encix1 < i) {
            bb_putbits(encoded1, ref encix1, 0xEC11, 16);
        }
        // doplnime ECC
        i = qrp[3] * qrp[4]; //ppoly, pmemptr , psize , plen , pblocks
        qr_rs(0x11D, encoded1, qrp[5] - i, i, qrp[4]);
        //Call arr2hexstr(encoded1)
        encix1 = qrp[5];

        // Pole pro vystup
        var qrarr = new byte[2][]; // 24 bytes per row
        qrarr[0] = new byte[qrp[2] * 24 + 24];
        qrarr[1] = new byte[qrp[2] * 24 + 24];
        qrarr[0][0] = 0;
        ch = 0;
        var qrsync1 = new byte[8];
        var qrsync2 = new byte[5];
        bb_putbits(qrsync1, ref ch, new byte[] { 0xFE, 0x82, 0xBA, 0xBA, 0xBA, 0x82, 0xFE, 0 }, 64);
        qr_mask(qrarr, qrsync1, 8, 0, 0); // sync UL
        qr_mask(qrarr, 0, 8, 8, 0); // fmtinfo UL under - bity 14..9 SYNC 8
        qr_mask(qrarr, qrsync1, 8, 0, siz - 7); // sync UR ( o bit vlevo )
        qr_mask(qrarr, 0, 8, 8, siz - 8); // fmtinfo UR - bity 7..0
        qr_mask(qrarr, qrsync1, 8, siz - 7, 0); // sync DL (zasahuje i do quiet zony)
        qr_mask(qrarr, 0, 8, siz - 8, 0); // blank nad DL

        bool x;
        for (i = 0; i <= 6; i++) {
            x = qr_bit(qrarr, -1, i, 8, 0);           // svisle fmtinfo UL - bity 0..5 SYNC 6,7
            x = qr_bit(qrarr, -1, i, siz - 8, 0);     // svisly blank pred UR
            x = qr_bit(qrarr, -1, siz - 1 - i, 8, 0); // svisle fmtinfo DL - bity 14..8
        }
        x = qr_bit(qrarr, -1, 7, 8, 0);       // svisle fmtinfo UL - bity 0..5 SYNC 6,7
        x = qr_bit(qrarr, -1, 7, siz - 8, 0); // svisly blank pred UR
        x = qr_bit(qrarr, -1, 8, 8, 0);       // svisle fmtinfo UL - bity 0..5 SYNC 6,7
        x = qr_bit(qrarr, -1, siz - 8, 8, 1); // black dot DL
        if (qrp[13] != 0 || qrp[14] != 0) { // versioninfo
            // UR ver 0 1 2;3 4 5;...;15 16 17
            // LL ver 0 3 6 9 12 15;1 4 7 10 13 16; 2 5 8 11 14 17
            k = 65536 * qrp[13] + 256 * qrp[14] + 1 * qrp[15];
            c = 0; r = 0;
            for (i = 0; i <= 17; i++) {
                ch = k % 2;
                x = qr_bit(qrarr, -1, r, siz - 11 + c, ch); // UR ver
                x = qr_bit(qrarr, -1, siz - 11 + c, r, ch); // DL ver
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
            x = qr_bit(qrarr, -1, i, 6, c); // vertical on column 6
            x = qr_bit(qrarr, -1, 6, i, c); // horizontal on row 6
            c = (c + 1) % 2;
        }

        // other syncs
        ch = 0;
        bb_putbits(qrsync2, ref ch, new byte[] { 0x1F, 0x11, 0x15, 0x11, 0x1F }, 40);
        ch = 6;
        while (ch > 0 && qrp[6 + ch] == 0) {
            ch--;
        }
        if (ch > 0) {
            for (c = 0; c <= ch; c++) {
                for (r = 0; r <= ch; r++) {
                    // corners
                    if ((c != 0 || r != 0) && (c != ch || r != 0) && (c != 0 || r != ch)) {
                        qr_mask(qrarr, qrsync2, 5, qrp[r + 6] - 2, qrp[c + 6] - 2);
                    }
                }
            }
        }

        // qr_fill(parr as Variant, psiz%, pb as Variant, pblocks%, pdlen%, ptlen%)
        // vyplni pole parr (psiz x 24 bytes) z pole pb pdlen = pocet dbytes, pblocks = bloku, ptlen celkem
        qr_fill(qrarr, siz, encoded1, qrp[4], qrp[5] - qrp[3] * qrp[4], qrp[5]);
        mask = 8; // auto
        i = poptions.IndexOf("mask=");
        if (i >= 0) {
            mask = poptions.Substring(i + 5, 1).ToCharArray()[0];
        }

        if (mask < 0 || mask > 7) {
            j = -1;
            for (mask = 0; mask <= 7; mask++) {
                addmm(qrarr, ecl, mask, siz);
                i = qr_xormask(qrarr, siz, mask, false);
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

        i = qr_xormask(qrarr, siz, mask, true);
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
        qr_bch_calc(ref k, 0x537);
        //MsgBox "mask :" & hex(k,3) & " " & hex(k xor &H5412,3)
        k = k ^ 0x5412; // micro xor &H4445
        var r = 0;
        var c = siz - 1;
        bool x;
        for (int i = 0; i <= 14; i++) {
            var ch = k % 2;
            k >>= 1;
            x = qr_bit(qrarr, -1, r, 8, ch); // svisle fmtinfo UL - bity 0..5 SYNC 6,7 .... 8..14 dole
            x = qr_bit(qrarr, -1, 8, c, ch); // vodorovne odzadu 0..7 ............ 8,SYNC,9..14
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

    public static void EncodeBarcode(string data, int para = 0) {
        if (null == mInstance) {
            mInstance = new QR();
        }
        var s = "mode=" + "MLQH".Substring(para % 4, 1);
        s = mInstance.qr_gen(data, s);
    }
}