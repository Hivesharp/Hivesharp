import { memo, useMemo } from 'react';
import {
  ReactFlow,
  Handle,
  Position,
  MarkerType,
  type Node,
  type Edge,
  type NodeTypes,
  type NodeProps,
} from '@xyflow/react';
import '@xyflow/react/dist/style.css';
import type { WorkflowDescriptor, WorkflowStepResult } from '@/types';

// --- Status configuration ---

const STATUS_CONFIG = {
  Completed: { border: '#22c55e', bg: '#22c55e12', shadow: '0 0 0 1px #22c55e40, 0 2px 8px #22c55e20', dot: '#22c55e' },
  Failed:    { border: '#ef4444', bg: '#ef444412', shadow: '0 0 0 1px #ef444440, 0 2px 8px #ef444420', dot: '#ef4444' },
  Skipped:   { border: '#6b7280', bg: '#6b728012', shadow: '0 0 0 1px #6b728040, 0 2px 8px #6b728020', dot: '#6b7280' },
  Suspended: { border: '#eab308', bg: '#eab30812', shadow: '0 0 0 1px #eab30840, 0 2px 8px #eab30820', dot: '#eab308' },
} as const;

const HANDLE_STYLE: React.CSSProperties = { opacity: 0, pointerEvents: 'none', width: 6, height: 6 };

// --- Node data types ---

interface StepNodeData { label: string; status?: string; [key: string]: unknown; }
interface DiamondNodeData { type: string; status?: string; [key: string]: unknown; }
interface ParallelNodeData { status?: string; [key: string]: unknown; }

// --- Status dot ---

function StatusDot({ color }: { color: string }) {
  return (
    <span
      style={{
        display: 'inline-block',
        width: 7,
        height: 7,
        borderRadius: '50%',
        background: color,
        flexShrink: 0,
      }}
    />
  );
}

// --- Step Node ---

const StepNode = memo(function StepNode({ data }: NodeProps) {
  const d = data as StepNodeData;
  const sc = d.status ? STATUS_CONFIG[d.status as keyof typeof STATUS_CONFIG] : null;
  return (
    <>
      <Handle type="target" position={Position.Top} style={HANDLE_STYLE} />
      <div
        style={{
          padding: '10px 0',
          borderRadius: 10,
          border: `2px solid ${sc?.border ?? 'var(--border)'}`,
          background: sc?.bg ?? 'var(--card)',
          color: 'var(--card-foreground)',
          fontSize: 13,
          fontWeight: 500,
          fontFamily: 'var(--font-mono, monospace)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          gap: 8,
          boxShadow: sc?.shadow ?? '0 1px 4px rgba(0,0,0,0.08)',
          width: 160,
          cursor: 'default',
          userSelect: 'none',
        }}
      >
        {sc && <StatusDot color={sc.dot} />}
        {d.label}
      </div>
      <Handle type="source" position={Position.Bottom} style={HANDLE_STYLE} />
    </>
  );
});

// --- Diamond Node (branch / merge) ---
// Container: 56×56. Inner square: 40×40 at (8,8) rotated 45°.
// Bounding box of rotated 40×40 ≈ 56.6 → diamond points land exactly on container edges.
// ReactFlow handles at Position.Top (28,0) and Position.Bottom (28,56) align with diamond tips.

const DiamondNode = memo(function DiamondNode({ data }: NodeProps) {
  const d = data as DiamondNodeData;
  const sc = d.status ? STATUS_CONFIG[d.status as keyof typeof STATUS_CONFIG] : null;
  const isBranch = d.type === 'branch';
  return (
    <>
      <Handle type="target" position={Position.Top} style={HANDLE_STYLE} />
      <div style={{ width: 56, height: 56, position: 'relative', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
        {/* Diamond shape */}
        <div
          style={{
            position: 'absolute',
            top: 8,
            left: 8,
            width: 40,
            height: 40,
            transform: 'rotate(45deg)',
            border: `2px solid ${sc?.border ?? 'var(--border)'}`,
            background: sc?.bg ?? 'var(--card)',
            borderRadius: 4,
            boxShadow: sc?.shadow ?? '0 1px 4px rgba(0,0,0,0.08)',
          }}
        />
        {/* Label over diamond */}
        {isBranch && (
          <span
            style={{
              position: 'relative',
              fontSize: 9,
              fontWeight: 800,
              color: sc?.border ?? 'hsl(var(--muted-foreground))',
              letterSpacing: '0.06em',
              textTransform: 'uppercase',
              userSelect: 'none',
              lineHeight: 1,
            }}
          >
            if
          </span>
        )}
      </div>
      <Handle type="source" position={Position.Bottom} style={HANDLE_STYLE} />
    </>
  );
});

// --- Parallel Node ---

const ParallelNode = memo(function ParallelNode({ data }: NodeProps) {
  const d = data as ParallelNodeData;
  const sc = d.status ? STATUS_CONFIG[d.status as keyof typeof STATUS_CONFIG] : null;
  return (
    <>
      <Handle type="target" position={Position.Top} style={HANDLE_STYLE} />
      <div
        style={{
          padding: '6px 16px',
          borderRadius: 20,
          border: `2px solid ${sc?.border ?? 'var(--border)'}`,
          background: sc?.bg ?? 'var(--card)',
          color: sc?.border ?? 'var(--muted-foreground)',
          fontSize: 11,
          fontWeight: 700,
          fontFamily: 'var(--font-mono, monospace)',
          display: 'flex',
          alignItems: 'center',
          gap: 7,
          boxShadow: sc?.shadow ?? '0 1px 4px rgba(0,0,0,0.08)',
          letterSpacing: '0.08em',
          textTransform: 'uppercase',
          whiteSpace: 'nowrap',
          cursor: 'default',
          userSelect: 'none',
        }}
      >
        {/* Three vertical bars icon */}
        <span style={{ display: 'flex', gap: 2, alignItems: 'center' }}>
          {[0, 1, 2].map((i) => (
            <span
              key={i}
              style={{ display: 'inline-block', width: 2, height: 12, background: 'currentColor', borderRadius: 1 }}
            />
          ))}
        </span>
        parallel
      </div>
      <Handle type="source" position={Position.Bottom} style={HANDLE_STYLE} />
    </>
  );
});

// nodeTypes must be defined outside the component to prevent unnecessary re-renders
const nodeTypes: NodeTypes = {
  step: StepNode,
  branch: DiamondNode,
  merge: DiamondNode,
  parallel: ParallelNode,
};

// --- Main component ---

interface WorkflowGraphProps {
  workflow: WorkflowDescriptor;
  stepResults?: WorkflowStepResult[];
}

export default function WorkflowGraph({ workflow, stepResults }: WorkflowGraphProps) {
  const statusMap = useMemo(() => {
    const map = new Map<string, string>();
    stepResults?.forEach((s) => map.set(s.stepId, s.status));
    return map;
  }, [stepResults]);

  const { nodes, edges } = useMemo(() => {
    const nodes: Node[] = [];
    const edges: Edge[] = [];

    const positions = layoutNodes(workflow);

    workflow.nodes.forEach((n) => {
      const pos = positions.get(n.id) ?? { x: 0, y: 0 };
      const status = statusMap.get(n.id);
      nodes.push({
        id: n.id,
        type: n.type,
        position: pos,
        data: { label: n.label ?? n.id, status, type: n.type },
      });
    });

    workflow.edges.forEach((e, i) => {
      edges.push({
        id: `e-${i}`,
        source: e.source,
        target: e.target,
        label: e.label ?? undefined,
        type: 'smoothstep',
        markerEnd: {
          type: MarkerType.ArrowClosed,
          width: 16,
          height: 16,
          color: 'var(--muted-foreground)',
        },
        style: { stroke: 'var(--muted-foreground)', strokeWidth: 1.5 },
        labelStyle: { fontSize: '11px', fontWeight: 600, fill: 'var(--foreground)' },
        labelBgStyle: { fill: 'var(--background)', fillOpacity: 0.9 },
        labelBgPadding: [6, 4] as [number, number],
        labelBgBorderRadius: 4,
      });
    });

    return { nodes, edges };
  }, [workflow, statusMap]);

  return (
    <div className="w-full h-full">
      <ReactFlow
        nodes={nodes}
        edges={edges}
        nodeTypes={nodeTypes}
        colorMode="light"
        fitView
        fitViewOptions={{ padding: 0.3, maxZoom: 1.2 }}
        panOnDrag
        zoomOnScroll
        zoomOnPinch
        nodesDraggable={false}
        nodesConnectable={false}
        proOptions={{ hideAttribution: true }}
        minZoom={0.2}
        maxZoom={2}
      />
    </div>
  );
}

// --- Layout ---

function layoutNodes(workflow: WorkflowDescriptor): Map<string, { x: number; y: number }> {
  const positions = new Map<string, { x: number; y: number }>();
  const adjacency = new Map<string, string[]>();
  const inDegree = new Map<string, number>();

  workflow.nodes.forEach((n) => {
    adjacency.set(n.id, []);
    inDegree.set(n.id, 0);
  });

  workflow.edges.forEach((e) => {
    adjacency.get(e.source)?.push(e.target);
    inDegree.set(e.target, (inDegree.get(e.target) ?? 0) + 1);
  });

  // Topological sort into layers (BFS by levels)
  const layers: string[][] = [];
  const queue: string[] = [];

  for (const [id, deg] of inDegree) {
    if (deg === 0) queue.push(id);
  }

  while (queue.length > 0) {
    const layer = [...queue];
    layers.push(layer);
    queue.length = 0;

    for (const id of layer) {
      for (const next of adjacency.get(id) ?? []) {
        const newDeg = (inDegree.get(next) ?? 1) - 1;
        inDegree.set(next, newDeg);
        if (newDeg === 0) queue.push(next);
      }
    }
  }

  const yGap = 110;
  const xGap = 220;

  layers.forEach((layer, layerIdx) => {
    const totalWidth = (layer.length - 1) * xGap;
    const startX = -totalWidth / 2;
    layer.forEach((id, nodeIdx) => {
      positions.set(id, {
        x: startX + nodeIdx * xGap,
        y: layerIdx * yGap,
      });
    });
  });

  return positions;
}
