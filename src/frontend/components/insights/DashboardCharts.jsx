import { Area, AreaChart, Bar, BarChart, CartesianGrid, Cell, Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { formatDuration, getGoalHistory } from './dashboardData';

const colors = { teal: '#2f9077', blue: '#557fbe', amber: '#c78a35', purple: '#8568b4' };
const shortDate = new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric' });
const dateLabel = value => shortDate.format(new Date(`${value}T00:00:00`));

function DashboardTooltip({ active, payload, label, mode }) {
  if (!active || !payload?.length) return null;
  const point = payload[0].payload;
  const lines = mode === 'goal' ? [['Actual', `${Math.round(point.actual)}%`], ['Expected', `${Math.round(point.expected)}%`]] : payload.map(item => [item.name, mode === 'time' ? formatDuration(item.value) : `${item.value} notes`]);
  return <div className="analytics-tooltip"><strong>{mode === 'goal' || mode === 'time' ? dateLabel(label) : point.name}</strong>{lines.map(([name, value]) => <span key={name}>{name}<b>{value}</b></span>)}</div>;
}

export function ActivityChart({ data }) {
  return <ResponsiveContainer width="100%" height={236}><AreaChart data={data} margin={{ top: 8, right: 4, left: -22, bottom: 0 }}><defs><linearGradient id="studyTime" x1="0" x2="0" y1="0" y2="1"><stop offset="5%" stopColor="#2f9077" stopOpacity=".38"/><stop offset="95%" stopColor="#2f9077" stopOpacity="0"/></linearGradient></defs><CartesianGrid vertical={false} stroke="#e5ece7"/><XAxis dataKey="date" tickFormatter={dateLabel} minTickGap={28} tickLine={false} axisLine={false}/><YAxis tickFormatter={value => `${value}m`} tickLine={false} axisLine={false}/><Tooltip content={<DashboardTooltip mode="time"/>}/><Area type="monotone" dataKey="minutes" name="Study time" stroke="#2f9077" fill="url(#studyTime)" strokeWidth={2.5}/></AreaChart></ResponsiveContainer>;
}

export function SubjectBarChart({ data, dataKey, name, onInspect }) {
  return <ResponsiveContainer width="100%" height={236}><BarChart data={data} layout="vertical" margin={{ top: 2, right: 10, left: 0, bottom: 0 }} onClick={event => event?.activePayload?.[0]?.payload && onInspect(event.activePayload[0].payload.id)}><CartesianGrid horizontal={false} stroke="#edf2ee"/><XAxis type="number" tickFormatter={value => dataKey === 'minutes' ? formatDuration(value) : value} tickLine={false} axisLine={false}/><YAxis type="category" dataKey="name" width={82} tickLine={false} axisLine={false}/><Tooltip content={<DashboardTooltip mode={dataKey === 'minutes' ? 'time' : 'notes'}/>}/><Bar dataKey={dataKey} name={name} radius={[0, 5, 5, 0]}>{data.map(item => <Cell key={item.id} fill={colors[item.color] ?? colors.teal}/>)}</Bar></BarChart></ResponsiveContainer>;
}

export function GoalPaceChart({ goal, notes }) {
  const data = getGoalHistory(goal, notes);
  if (!data.length) return <p className="goal-chart-empty">Log this metric in a study note to see pace over time.</p>;
  return <ResponsiveContainer width="100%" height={126}><LineChart data={data} margin={{ top: 8, right: 4, left: -24, bottom: 0 }}><XAxis dataKey="date" tickFormatter={dateLabel} minTickGap={36} tickLine={false} axisLine={false}/><YAxis unit="%" domain={[0, 100]} tickLine={false} axisLine={false}/><Tooltip content={<DashboardTooltip mode="goal"/>}/><Line type="monotone" dataKey="expected" name="Expected" stroke="#ba8753" strokeDasharray="5 4" dot={false}/><Line type="monotone" dataKey="actual" name="Actual" stroke="#2f9077" strokeWidth={2.5} dot={{ r: 3 }}/></LineChart></ResponsiveContainer>;
}

export default function DashboardCharts({ kind, ...props }) {
  if (kind === 'activity') return <ActivityChart {...props}/>;
  if (kind === 'subject') return <SubjectBarChart {...props}/>;
  return <GoalPaceChart {...props}/>;
}
