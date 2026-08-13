import { Bar, BarChart, Cell, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';

const colors = { teal: '#2e9483', blue: '#547fc8', amber: '#c58c35', purple: '#8b68b5' };

function TooltipContent({ active, payload }) {
  if (!active || !payload?.length) return null;
  const point = payload[0].payload;
  return <div className="dashboard-tooltip"><strong>{point.name}</strong><span>{point.count} notes <b>{point.percentage.toFixed(1)}%</b></span></div>;
}

export function NotesDistributionChart({ data, onInspectSubject }) {
  if (!data.length) return <p className="dashboard-empty">Create a subject to see how your notes are distributed.</p>;
  return <ResponsiveContainer width="100%" height={Math.max(220, data.length * 42)}><BarChart data={data} layout="vertical" margin={{ top: 0, right: 18, left: 0, bottom: 0 }} onClick={event => event?.activePayload?.[0]?.payload && onInspectSubject(event.activePayload[0].payload.id)}><XAxis type="number" allowDecimals={false} tickLine={false} axisLine={false}/><YAxis type="category" dataKey="name" width={98} tickLine={false} axisLine={false}/><Tooltip content={<TooltipContent/>}/><Bar dataKey="count" name="Notes" radius={[0, 5, 5, 0]}>{data.map(subject => <Cell key={subject.id} fill={colors[subject.color] ?? colors.teal}/>)}</Bar></BarChart></ResponsiveContainer>;
}
