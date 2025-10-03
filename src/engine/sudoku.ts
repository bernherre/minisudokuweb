export class SudokuEngine {
    N: number; boxRows: number; boxCols: number; private _s = 1;
    constructor(size: 4 | 6 = 4, seed?: number) {
        if (![4, 6].includes(size)) throw new Error('Solo 4x4 o 6x6');
        this.N = size;
        if (size === 4) { this.boxRows = 2; this.boxCols = 2; }
        else { this.boxRows = 3; this.boxCols = 2; } // 6x6 => 3x2
        this._s = seed ?? Math.floor(Math.random() * 1e9);
    }
    private rng() { this._s = (this._s * 48271) % 0x7fffffff; return this._s / 0x7fffffff }
    private shuffle<T>(a: T[]) { const x = a.slice(); for (let i = x.length - 1; i > 0; i--) { const j = Math.floor(this.rng() * (i + 1));[x[i], x[j]] = [x[j], x[i]] } return x }
    empty() { return Array.from({ length: this.N }, () => Array(this.N).fill(0)) }
    clone(g: number[][]) { return g.map(r => r.slice()) }
    isValid(g: number[][], r: number, c: number, v: number) {
        if (v < 1 || v > this.N) return false;
        for (let i = 0; i < this.N; i++) { if (g[r][i] === v) return false; if (g[i][c] === v) return false; }
        const sr = Math.floor(r / this.boxRows) * this.boxRows, sc = Math.floor(c / this.boxCols) * this.boxCols;
        for (let rr = 0; rr < this.boxRows; rr++) for (let cc = 0; cc < this.boxCols; cc++) { if (g[sr + rr][sc + cc] === v) return false; }
        return true;
    }
    private findEmpty(g: number[][]) { for (let r = 0; r < this.N; r++) for (let c = 0; c < this.N; c++) if (g[r][c] === 0) return [r, c] as const; return null }
    solve(g: number[][]): boolean {
        const pos = this.findEmpty(g); if (!pos) return true; const [r, c] = pos;
        for (const v of this.shuffle([...Array(this.N)].map((_, i) => i + 1))) { if (this.isValid(g, r, c, v)) { g[r][c] = v; if (this.solve(g)) return true; g[r][c] = 0 } } return false
    }
    countSolutions(g: number[][], limit = 2) {
        let cnt = 0; const dfs = () => {
            if (cnt >= limit) return; const p = this.findEmpty(g); if (!p) { cnt++; return; } const [r, c] = p;
            for (let v = 1; v <= this.N && cnt < limit; v++) { if (this.isValid(g, r, c, v)) { g[r][c] = v; dfs(); g[r][c] = 0 } }
        }; dfs(); return cnt
    }
    generateSolved() { const g = this.empty(); this.solve(g); return g }
    generatePuzzle(clues: number) {
        const solved = this.generateSolved(); const puzzle = this.clone(solved);
        const cells = [...Array(this.N * this.N).keys()];
        for (const idx of this.shuffle(cells)) {
            if (puzzle.flat().filter(x => x !== 0).length <= clues) break;
            const r = Math.floor(idx / this.N), c = idx % this.N; const bak = puzzle[r][c]; if (bak === 0) continue;
            const rowFilled = puzzle[r].filter(x => x !== 0).length; const colFilled = puzzle.map(rr => rr[c]).filter(x => x !== 0).length;
            if (rowFilled <= this.N / 2 || colFilled <= this.N / 2) continue;
            puzzle[r][c] = 0; const t = this.clone(puzzle); if (this.countSolutions(t, 2) !== 1) puzzle[r][c] = bak;
        }
        return { puzzle, solved };
    }
    isCompleteAndValid(g: number[][]) {
        const N = this.N;
        for (let r = 0; r < N; r++) {
            const R = new Set<number>(), C = new Set<number>(); for (let c = 0; c < N; c++) {
                const vr = g[r][c], vc = g[c][r];
                if (vr < 1 || vr > N || R.has(vr)) return false; R.add(vr); if (vc < 1 || vc > N || C.has(vc)) return false; C.add(vc);
            }
        }
        for (let br = 0; br < N; br += this.boxRows) {
            for (let bc = 0; bc < N; bc += this.boxCols) {
                const B = new Set<number>(); for (let r = 0; r < this.boxRows; r++) for (let c = 0; c < this.boxCols; c++) { const v = g[br + r][bc + c]; if (v < 1 || v > N || B.has(v)) return false; B.add(v); }
            }
        }
        return true;
    }
}
export const DIFFICULTY: Record<string, (N: number) => [number, number]> = {
    'Fácil': (N) => [Math.floor(N * N * 0.65), Math.floor(N * N * 0.80)],
    'Media': (N) => [Math.floor(N * N * 0.50), Math.floor(N * N * 0.65)],
    'Difícil': (N) => [Math.floor(N * N * 0.35), Math.floor(N * N * 0.50)]
}
