import { describe, it, expect } from "vitest";
import { SudokuEngine } from "./sudoku";

describe("SudokuEngine", () => {
    it("genera un tablero resuelto válido 4x4", () => {
        const eng = new SudokuEngine(4);
        const solved = eng.generateSolved();
        expect(eng.isCompleteAndValid(solved)).toBe(true);
    });

    it("genera un puzzle 6x6 y la solución es válida", () => {
        const eng = new SudokuEngine(6);
        const { solved } = eng.generatePuzzle(20);
        expect(eng.isCompleteAndValid(solved)).toBe(true);
    });
});
