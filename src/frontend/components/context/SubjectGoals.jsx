import { useState } from 'react';
import { CalendarDays, Check, Plus, Target, Trash2 } from '../../icons';

const clamp = value => Math.max(0, Math.min(100, value));
const dateFormatter = new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
const numberFormatter = new Intl.NumberFormat(undefined, { maximumFractionDigits: 2 });
const parseDecimal = value => {
  const trimmed = value.trim();
  if (!trimmed) return Number.NaN;
  const normalized = trimmed.includes(',') ? trimmed.replaceAll('.', '').replace(',', '.') : trimmed;
  return Number(normalized);
};
const sanitizeDecimal = value => {
  const cleaned = value.replace(/[^0-9,\.]/g, '');
  const separator = cleaned.search(/[,\.]/);
  if (separator < 0) return cleaned;
  return `${cleaned.slice(0, separator + 1)}${cleaned.slice(separator + 1).replace(/[,\.]/g, '')}`;
};
const periodLabels = { 0: 'All time', 1: 'Daily', 2: 'Weekly', 3: 'Monthly', 4: 'Custom period' };
const notesInGoalPeriod = (notes, goal) => notes.filter(note => {
  const studiedOn = note.studyStartedAtUtc.slice(0, 10);
  return (!goal.periodStartDate || studiedOn >= goal.periodStartDate) && (!goal.periodEndDate || studiedOn <= goal.periodEndDate);
});

function GoalRing({ percent, overdue = false }) {
  const radius = 18;
  const circumference = 2 * Math.PI * radius;
  return <div className={`goal-ring ${overdue ? 'overdue' : ''}`}><svg viewBox="0 0 48 48" aria-hidden="true"><circle cx="24" cy="24" r={radius}/><circle className="goal-ring-progress" cx="24" cy="24" r={radius} style={{ strokeDasharray: circumference, strokeDashoffset: circumference * (1 - percent / 100) }}/></svg><strong>{Math.round(percent)}%</strong></div>;
}

function GoalTrend({ goal, notes }) {
  const points = notesInGoalPeriod(notes, goal)
    .filter(note => note.metrics.some(metric => metric.definition.id === goal.metricDefinition?.id))
    .toSorted((left, right) => new Date(left.studyStartedAtUtc) - new Date(right.studyStartedAtUtc))
    .reduce((series, note) => [...series, (series.at(-1) ?? 0) + (note.metrics.find(metric => metric.definition.id === goal.metricDefinition?.id)?.value ?? 0)], []);
  if (!points.length) return <p className="goal-trend-empty">Add this metric to a study note to start the progress graph.</p>;
  const maximum = Math.max(goal.targetValue, ...points, 1);
  const path = points.map((value, index) => `${(index / Math.max(1, points.length - 1)) * 100},${42 - (value / maximum) * 36}`).join(' ');
  return <div className="goal-trend"><div><span>Progress history</span><small>{points.length} logged study {points.length === 1 ? 'entry' : 'entries'}</small></div><svg viewBox="0 0 100 48" preserveAspectRatio="none" role="img" aria-label="Cumulative metric progress"><path className="goal-trend-baseline" d="M0 42H100"/><polyline points={path}/>{points.map((value, index) => <circle key={`${value}-${index}`} cx={(index / Math.max(1, points.length - 1)) * 100} cy={42 - (value / maximum) * 36} r="1.8"/>)}</svg></div>;
}

function GoalProgress({ goal, notes, onSetSubGoalCompletion }) {
  if (goal.kind === 1) {
    const percent = clamp((goal.currentValue / goal.targetValue) * 100);
    const remaining = Math.max(0, goal.targetValue - goal.currentValue);
    const sessions = notesInGoalPeriod(notes, goal).filter(note => note.metrics.some(metric => metric.definition.id === goal.metricDefinition?.id)).length;
    const average = sessions ? goal.currentValue / sessions : 0;
    const period = goal.periodStartDate && goal.periodEndDate ? `${dateFormatter.format(new Date(`${goal.periodStartDate}T00:00:00`))} – ${dateFormatter.format(new Date(`${goal.periodEndDate}T00:00:00`))}` : periodLabels[goal.period] ?? 'All time';
    return <div className="goal-visual"><GoalRing percent={percent}/><div className="goal-visual-main"><div className="goal-meta"><span>{goal.metricDefinition?.name}</span><strong>{numberFormatter.format(goal.currentValue)} / {numberFormatter.format(goal.targetValue)}</strong></div><div className="goal-progress"><i style={{ width: `${percent}%` }}/></div><div className="goal-insights"><span>{remaining ? `${numberFormatter.format(remaining)} remaining` : 'Target reached'}</span><b className={percent >= 100 ? 'complete' : 'on-pace'}>{percent >= 100 ? 'Complete' : `${numberFormatter.format(average)} / session`}</b></div><small>{period}</small></div><GoalTrend goal={goal} notes={notes}/></div>;
  }
  const subGoals = goal.subGoals ?? [];
  if (subGoals.length) {
    const completed = subGoals.filter(subGoal => subGoal.isCompleted).length;
    const percent = completed / subGoals.length * 100;
    return <div className="goal-visual"><GoalRing percent={percent}/><div className="goal-visual-main"><div className="goal-meta"><span>Completion goal</span><strong>{completed} / {subGoals.length} steps</strong></div><div className="goal-progress"><i style={{ width: `${percent}%` }}/></div><div className="goal-insights"><span>{Math.round(percent)}% complete</span><b className={percent === 100 ? 'complete' : 'on-pace'}>{percent === 100 ? 'Complete' : 'In progress'}</b></div><div className="sub-goal-list">{subGoals.map(subGoal => <label key={subGoal.id}><input type="checkbox" checked={subGoal.isCompleted} onChange={event => onSetSubGoalCompletion(subGoal.id, event.target.checked)}/><span>{subGoal.title}</span></label>)}</div></div></div>;
  }
  if (goal.isCompleted) return <div className="goal-visual"><GoalRing percent={100}/><div className="goal-visual-main"><div className="goal-meta"><span>Completion goal</span><strong>Done</strong></div><div className="goal-progress"><i style={{ width: '100%' }}/></div><div className="goal-insights"><span>{goal.completedAtUtc ? `Completed ${dateFormatter.format(new Date(goal.completedAtUtc))}` : 'Completed'}</span><b className="complete">Complete</b></div></div></div>;
  if (!goal.targetDate) return <div className="goal-visual"><GoalRing percent={0}/><div className="goal-visual-main"><div className="goal-meta"><span>{periodLabels[goal.period] ?? 'All time'}</span><strong>In progress</strong></div><div className="goal-progress"><i style={{ width: '0%' }}/></div><div className="goal-insights"><span>Mark this goal done when you finish it.</span><b className="on-pace">Open</b></div></div></div>;
  const target = new Date(`${goal.targetDate}T00:00:00`);
  const start = new Date(goal.createdAtUtc);
  const total = Math.max(1, target - start);
  const elapsed = clamp(((Date.now() - start) / total) * 100);
  const remainingDays = Math.ceil((target - Date.now()) / 86_400_000);
  return <div className="goal-visual"><GoalRing percent={elapsed} overdue={remainingDays < 0}/><div className="goal-visual-main"><div className="goal-meta"><span>Due {dateFormatter.format(target)}</span><strong>{remainingDays >= 0 ? `${remainingDays} days left` : `${Math.abs(remainingDays)} days overdue`}</strong></div><div className={`goal-progress ${remainingDays < 0 ? 'overdue' : ''}`}><i style={{ width: `${elapsed}%` }}/></div><div className="goal-insights"><span>{Math.round(elapsed)}% of timeline elapsed</span><b className={remainingDays < 0 ? 'overdue' : 'on-pace'}>{remainingDays < 0 ? 'Needs attention' : 'On schedule'}</b></div></div></div>;
}

export function SubjectGoals({ goals, notes, topics = [], metricDefinitions, onCreate, onRemove, onComplete, onSetSubGoalCompletion }) {
  const [open, setOpen] = useState(false);
  const [kind, setKind] = useState(1);
  const [title, setTitle] = useState('');
  const [topicId, setTopicId] = useState('');
  const [metricDefinitionId, setMetricDefinitionId] = useState('');
  const [targetValue, setTargetValue] = useState('');
  const [targetDate, setTargetDate] = useState('');
  const [period, setPeriod] = useState(0);
  const [periodStartDate, setPeriodStartDate] = useState('');
  const [periodEndDate, setPeriodEndDate] = useState('');
  const [subGoals, setSubGoals] = useState(['']);
  const save = async event => {
    event.preventDefault();
    const goal = { topicId: topicId || topics[0]?.id || null, title, kind, metricDefinitionId: kind === 1 ? metricDefinitionId || null : null, targetValue: kind === 1 ? parseDecimal(targetValue) : null, targetDate: targetDate || null, period, periodStartDate: period === 4 ? periodStartDate || null : null, periodEndDate: period === 4 ? periodEndDate || null : null, subGoals: kind === 2 ? subGoals.map(subGoal => subGoal.trim()).filter(Boolean) : [] };
    if (!goal.topicId || !title.trim() || (kind === 1 && (!metricDefinitionId || !Number.isFinite(goal.targetValue) || goal.targetValue <= 0)) || (period === 4 && (!periodStartDate || !periodEndDate || periodStartDate > periodEndDate))) return;
    if (await onCreate({ ...goal, title: title.trim() })) { setOpen(false); setTitle(''); setMetricDefinitionId(''); setTargetValue(''); setTargetDate(''); setPeriod(0); setPeriodStartDate(''); setPeriodEndDate(''); setSubGoals(['']); }
  };
  return <section className="subject-goals"><div className="subject-goals-head"><div><span>GOALS</span><small>Track progress for this subject.</small></div><button type="button" className="text-button" onClick={() => setOpen(value => !value)}><Plus size={15}/> Add goal</button></div>
    {open ? <form className="goal-composer" onSubmit={save}><label>Topic<select value={topicId} onChange={event => setTopicId(event.target.value)} required><option value="">Choose a topic</option>{topics.map(topic => <option key={topic.id} value={topic.id}>{topic.name}</option>)}</select></label><label>Goal title<input value={title} onChange={event => setTitle(event.target.value)} placeholder="What are you aiming for?" maxLength="256"/></label><fieldset><legend>Goal type</legend><label><input type="radio" checked={kind === 1} onChange={() => setKind(1)}/> Metric target</label><label><input type="radio" checked={kind === 2} onChange={() => setKind(2)}/> Completion goal</label></fieldset>{kind === 1 ? <div className="goal-fields"><select value={metricDefinitionId} onChange={event => setMetricDefinitionId(event.target.value)}><option value="">Choose a metric</option>{metricDefinitions.map(definition => <option key={definition.id} value={definition.id}>{definition.name}</option>)}</select><input type="text" inputMode="decimal" value={targetValue} onChange={event => setTargetValue(sanitizeDecimal(event.target.value))} placeholder="Target value"/></div> : <><label>Due date (optional)<input type="date" value={targetDate} onChange={event => setTargetDate(event.target.value)}/></label><div className="sub-goal-composer"><span>Sub-goals</span>{subGoals.map((subGoal, index) => <input key={index} value={subGoal} onChange={event => setSubGoals(items => items.map((item, itemIndex) => itemIndex === index ? event.target.value : item))} placeholder={`Step ${index + 1}`}/>)}</div><button type="button" className="text-button" onClick={() => setSubGoals(items => [...items, ''])}><Plus size={14}/> Add sub-goal</button></>}<label>Period<select value={period} onChange={event => setPeriod(Number(event.target.value))}>{Object.entries(periodLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>{period === 4 ? <div className="goal-fields"><label>Start date<input type="date" value={periodStartDate} onChange={event => setPeriodStartDate(event.target.value)}/></label><label>End date<input type="date" value={periodEndDate} onChange={event => setPeriodEndDate(event.target.value)}/></label></div> : null}<div><button type="button" className="ghost-button" onClick={() => setOpen(false)}>Cancel</button><button className="primary-button">Save goal</button></div></form> : null}
    <div className="goal-list">{goals.map(goal => <article className="goal-card" key={goal.id}><div className="goal-title"><span>{goal.kind === 1 ? <Target size={15}/> : <CalendarDays size={15}/>}</span><strong>{goal.title}</strong>{goal.kind !== 1 && !goal.subGoals?.length && !goal.isCompleted ? <button type="button" aria-label={`Complete ${goal.title}`} onClick={() => onComplete(goal.id)}><Check size={14}/></button> : null}<button type="button" aria-label={`Remove ${goal.title}`} onClick={() => onRemove(goal.id)}><Trash2 size={14}/></button></div><GoalProgress goal={goal} notes={notes} onSetSubGoalCompletion={onSetSubGoalCompletion}/></article>)}{goals.length === 0 ? <p className="goal-empty">Set a metric or completion goal to see your progress here.</p> : null}</div>
  </section>;
}
