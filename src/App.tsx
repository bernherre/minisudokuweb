
import React from "react";
import Board from "./components/Board";
import { SudokuEngine, DIFFICULTY } from "./engine/sudoku";

const pad = (n: number) => (n < 10 ? `0${n}` : `${n}`);
const fmtTime = (ms: number) => {
  const s = Math.floor(ms / 1000);
  const m = Math.floor(s / 60);
  const ss = s % 60;
  return `${pad(m)}:${pad(ss)}`;
};

export default function App(){
  const [size, setSize] = React.useState<4|6>(6);
  const [difficulty, setDifficulty] = React.useState<"Fácil"|"Media"|"Difícil">("Media");
  const [engine, setEngine] = React.useState(()=> new SudokuEngine(6));

  const init = React.useMemo(()=>{
    const [mi, ma] = DIFFICULTY[difficulty](size);
    return engine.generatePuzzle(Math.floor((mi+ma)/2));
  }, []);

  const [grid, setGrid] = React.useState<number[][]>(init.puzzle.map(r=>r.slice()));
  const [solution, setSolution] = React.useState<number[][]>(init.solved.map(r=>r.slice()));
  const [fixed, setFixed] = React.useState<boolean[][]>(init.puzzle.map(row=>row.map(v=>v!==0)));
  const [feedback, setFeedback] = React.useState<Record<string, "ok"|"bad"|"hint"|"fixed"|undefined>>({});
  const [startAt, setStartAt] = React.useState(Date.now());
  const [now, setNow] = React.useState(Date.now());
  const [won, setWon] = React.useState(false);

  React.useEffect(()=>{ const id=setInterval(()=>setNow(Date.now()), 500); return ()=>clearInterval(id); }, []);

  const resetGame = React.useCallback((newSize:4|6=size, newDifficulty:"Fácil"|"Media"|"Difícil"=difficulty)=>{
    const eng = new SudokuEngine(newSize);
    const [mi, ma] = DIFFICULTY[newDifficulty](newSize);
    const pack = eng.generatePuzzle(Math.floor((mi+ma)/2));
    setEngine(eng);
    setSize(newSize);
    setDifficulty(newDifficulty);
    setGrid(pack.puzzle.map(r=>r.slice()));
    setSolution(pack.solved.map(r=>r.slice()));
    setFixed(pack.puzzle.map(row=>row.map(v=>v!==0)));
    setFeedback({});
    setStartAt(Date.now());
    setWon(false);
  }, [size, difficulty]);

  const onSizeChange = (e:React.ChangeEvent<HTMLSelectElement>) => resetGame(Number(e.target.value) as 4|6, difficulty);
  const onDifficultyChange = (e:React.ChangeEvent<HTMLSelectElement>) => resetGame(size, e.target.value as "Fácil"|"Media"|"Difícil");

  const setValue = (r:number, c:number, v:number)=>{
    if (fixed[r][c]) return;
    const ng = grid.map(row=>row.slice());
    ng[r][c] = v;
    setGrid(ng);
    const key = `${r},${c}`;
    setFeedback(f=> ({...f, [key]: v===0? undefined : solution[r][c]===v? "ok":"bad"}));
  };

  const handleCellInput = (r:number, c:number, raw:string)=>{
    const ch = (raw||"").trim();
    if(ch===""){ setValue(r,c,0); return; }
    if(!/^\d$/.test(ch)) return;
    const v = parseInt(ch,10);
    if(v<1 || v>size) return;
    setValue(r,c,v);
  };

  const handleCheck = ()=>{
    let anyBad=false, anyEmpty=false;
    const fb: Record<string,"ok"|"bad"> = {};
    for(let r=0;r<size;r++){
      for(let c=0;c<size;c++){
        if(grid[r][c]===0){ anyEmpty=true; continue; }
        const k=`${r},${c}`;
        fb[k] = grid[r][c]===solution[r][c] ? "ok" : "bad";
        if (fb[k]==="bad") anyBad=true;
      }
    }
    setFeedback(fb);
    if(!anyBad && !anyEmpty){
      if(new SudokuEngine(size).isCompleteAndValid(grid)) setWon(true);
    }
  };

  const handleSolve = ()=>{
    setGrid(solution.map(r=>r.slice()));
    const fb: Record<string,"ok"> = {};
    for(let r=0;r<size;r++) for(let c=0;c<size;c++) fb[`${r},${c}`]="ok";
    setFeedback(fb);
    setWon(true);
  };

  const handleClear = ()=>{
    const ng = grid.map((row,r)=> row.map((v,c)=> fixed[r][c]? v : 0));
    setGrid(ng); setFeedback({}); setWon(false); setStartAt(Date.now());
  };

  const elapsed = now - startAt;
  const boxRows = size===4 ? 2 : 3;
  const boxCols = 2;

  return (
    <div className="app">
      <div className="card toolbar">
        <div className="controls">
          <label> Tamaño:&nbsp;
            <select value={size} onChange={onSizeChange}>
              <option value={4}>4×4</option>
              <option value={6}>6×6</option>
            </select>
          </label>
          <label> Dificultad:&nbsp;
            <select value={difficulty} onChange={onDifficultyChange}>
              {Object.keys(DIFFICULTY).map(k => <option key={k} value={k}>{k}</option>)}
            </select>
          </label>
          <button onClick={()=>resetGame(size, difficulty)}>Nuevo</button>
          <button onClick={handleCheck}>Verificar</button>
          <button onClick={handleSolve}>Resolver</button>
          <button onClick={handleClear}>Limpiar</button>
        </div>
        <div className="status">Tiempo: <span className="kbd">{fmtTime(elapsed)}</span> {won && <span>· ¡Completado! 🎉</span>}</div>
      </div>

      <div className="card">
        <Board
          size={size}
          boxRows={boxRows}
          boxCols={boxCols}
          grid={grid}
          fixed={fixed}
          feedback={feedback}
          onInput={handleCellInput}
        />
      </div>

      <div className="footer">Hecho con React + Vite. Usa teclas 1–{size} y <span className="kbd">Backspace</span> para borrar.</div>
    </div>
  );
}
