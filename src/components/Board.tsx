
import React from "react";

type BoardProps = {
  size: 4 | 6;
  boxRows: number;
  boxCols: number;
  grid: number[][];
  fixed: boolean[][];
  feedback: Record<string, "ok" | "bad" | "hint" | "fixed" | undefined>;
  onInput: (r: number, c: number, v: string) => void;
};

export default function Board({ size, boxRows, boxCols, grid, fixed, feedback, onInput }: BoardProps){
  // grid de cajas: (N/boxRows) x (N/boxCols)
  const boxGridStyle: React.CSSProperties = {
    display: "grid",
    gridTemplateColumns: `repeat(${size/boxCols}, auto)`,
    gridTemplateRows: `repeat(${size/boxRows}, auto)`,
    gap: "10px",
    justifyContent: "center"
  };

  const boxes = [];
  for(let br=0; br<size; br+=boxRows){
    for(let bc=0; bc<size; bc+=boxCols){
      const cells = [];
      for(let r=0; r<boxRows; r++){
        for(let c=0; c<boxCols; c++){
          const rr = br + r, cc = bc + c;
          const key = `${rr},${cc}`;
          const cls = ["cell"];
          if (fixed[rr][cc]) cls.push("fixed");
          const f = feedback[key];
          if (f === "ok") cls.push("ok");
          else if (f === "bad") cls.push("bad");

          cells.push(
            <Cell
              key={key}
              r={rr}
              c={cc}
              v={grid[rr][cc]}
              className={cls.join(" ")}
              editable={!fixed[rr][cc]}
              onInput={onInput}
            />
          )
        }
      }
      boxes.push(
        <div key={`${br}-${bc}`} className="box" style={{
          gridTemplateColumns: `repeat(${boxCols}, 1fr)`,
          gridTemplateRows: `repeat(${boxRows}, 1fr)`
        }}>
          {cells}
        </div>
      )
    }
  }

  return <div className="board" style={boxGridStyle}>{boxes}</div>;
}

function Cell({ r, c, v, className, editable, onInput }:{
  r:number; c:number; v:number; className:string; editable:boolean;
  onInput: (r:number, c:number, v:string)=>void
}){
  const ref = React.useRef<HTMLDivElement|null>(null);

  React.useEffect(()=>{
    const el = ref.current; if(!el) return;
    const onKey = (e:KeyboardEvent)=>{
      if(!editable) return;
      if(e.key === "Backspace" || e.key === "Delete"){ onInput(r,c,""); e.preventDefault(); return; }
      if(/^\d$/.test(e.key)){ onInput(r,c,e.key); e.preventDefault(); return; }
    };
    el.addEventListener("keydown", onKey);
    return ()=> el.removeEventListener("keydown", onKey);
  }, [editable, r, c, onInput]);

  return <div ref={ref} className={className} tabIndex={0} onClick={(e)=>e.currentTarget.focus()}>
    {v !== 0 ? v : ""}
  </div>;
}
