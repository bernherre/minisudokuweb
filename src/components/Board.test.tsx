import { render, screen } from "@testing-library/react";
import { describe, it, expect } from "vitest";
import React from "react";
import Board from "./Board";

describe("Board", () => {
    it("renderiza 16 celdas para 4x4", () => {
        const size = 4;
        const grid = Array.from({ length: size }, () => Array(size).fill(0));
        const fixed = grid.map(row => row.map(() => false));
        render(<Board size={4} boxRows={2} boxCols={2} grid={grid} fixed={fixed} feedback={{}} onInput={() => { }} />);
        const cells = screen.getAllByRole("button");
        expect(cells.length).toBe(16);
    });
});
