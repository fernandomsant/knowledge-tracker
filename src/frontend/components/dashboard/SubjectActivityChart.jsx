import { Bar, BarChart, Cell, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';

const COLORS = { teal: '#2e9483', blue: '#547fc8', amber: '#c58c35', purple: '#8b68b5' };

function ActivityTooltip({ active, payload }) {
  if (!active || !payload?.length) return null;
  const subject = payload[0].payload;
  return <div className="behavior-chart-tooltip"><strong>{subject.name}</strong><span>{subject.count} study {subject.count === 1 ? 'note' : 'notes'} · {subject.percentage.toFixed(1)}%</span></div>;
}

export function SubjectActivityChart({ data, totalNotes, onInspectSubject }) {
  if (!totalNotes) return <div className="behavior-empty"><span>No study notes in this range.</span></div>;
  const height = Math.min(246, Math.max(138, data.length * 27));
  return <ResponsiveContainer width="100%" height={height}><BarChart data={data} layout="vertical" margin={{ top: 2, right: 14, left: 0, bottom: 0 }} onClick={event => event?.activePayload?.[0]?.payload && onInspectSubject(event.activePayload[0].payload.id)}><XAxis type="number" allowDecimals={false} hide/><YAxis type="category" dataKey="name" width={85} tickLine={false} axisLine={false}/><Tooltip cursor={{ fill: '#f3f7f4' }} content={<ActivityTooltip/>}/><Bar dataKey="count" radius={[0, 5, 5, 0]}>{data.map(subject => <Cell key={subject.id} fill={COLORS[subject.color] ?? COLORS.teal}/>)}</Bar></BarChart></ResponsiveContainer>;
}
