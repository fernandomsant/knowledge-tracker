import { useState } from 'react';
import { CalendarDays, Check, Pencil, Plus, Target, Trash2 } from '../../icons';
import { TopicComposer } from './TopicComposer';

const clamp = value => Math.max(0, Math.min(100, value));
const dateFormatter = new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
const numberFormatter = new Intl.NumberFormat(undefined, { maximumFractionDigits: 2 });
const studyTimeMetricName = 'STUDY TIME';
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
const studyDurationInHours = duration => {
  const [hours = 0, minutes = 0, seconds = 0] = duration.split(':').map(Number);
  return hours + minutes / 60 + seconds / 3600;
};
const isStudyTimeGoal = goal => goal.metricDefinition?.name?.trim().toUpperCase() === studyTimeMetricName;
const goalValueForNote = (note, goal) => isStudyTimeGoal(goal)
  ? studyDurationInHours(note.studyDuration)
  : note.metrics.find(metric => metric.definition.id === goal.metricDefinition?.id)?.value ?? 0;

function GoalRing({ percent, overdue = false }) {
  const radius = 18;
  const circumference = 2 * Math.PI * radius;
  return <div className={`goal-ring ${overdue ? 'overdue' : ''}`}><svg viewBox="0 0 48 48" aria-hidden="true"><circle cx="24" cy="24" r={radius}/><circle className="goal-ring-progress" cx="24" cy="24" r={radius} style={{ strokeDasharray: circumference, strokeDashoffset: circumference * (1 - percent / 100) }}/></svg><strong>{Math.round(percent)}%</strong></div>;
}

function GoalTrend({ goal, notes }) {
  const points = notesInGoalPeriod(notes, goal)
    .filter(note => goalValueForNote(note, goal) > 0)
    .toSorted((left, right) => new Date(left.studyStartedAtUtc) - new Date(right.studyStartedAtUtc))
    .reduce((series, note) => [...series, (series.at(-1) ?? 0) + goalValueForNote(note, goal)], []);
  if (!points.length) return <p className="goal-trend-empty">{isStudyTimeGoal(goal) ? 'Record study time on a note to start the progress graph.' : 'Add this metric to a study note to start the progress graph.'}</p>;
  const maximum = Math.max(goal.targetValue, ...points, 1);
  const path = points.map((value, index) => `${(index / Math.max(1, points.length - 1)) * 100},${42 - (value / maximum) * 36}`).join(' ');
  return <div className="goal-trend"><div><span>Progress history</span><small>{points.length} logged study {points.length === 1 ? 'entry' : 'entries'}</small></div><svg viewBox="0 0 100 48" preserveAspectRatio="none" role="img" aria-label="Cumulative metric progress"><path className="goal-trend-baseline" d="M0 42H100"/><polyline points={path}/>{points.map((value, index) => <circle key={`${value}-${index}`} cx={(index / Math.max(1, points.length - 1)) * 100} cy={42 - (value / maximum) * 36} r="1.8"/>)}</svg></div>;
}

function GoalProgress({ goal, notes, onSetSubGoalCompletion }) {
  if (goal.kind === 1) {
    const percent = clamp((goal.currentValue / goal.targetValue) * 100);
    const remaining = Math.max(0, goal.targetValue - goal.currentValue);
    const sessions = notesInGoalPeriod(notes, goal).filter(note => goalValueForNote(note, goal) > 0).length;
    const average = sessions ? goal.currentValue / sessions : 0;
    const period = goal.periodStartDate && goal.periodEndDate ? `${dateFormatter.format(new Date(`${goal.periodStartDate}T00:00:00`))} – ${dateFormatter.format(new Date(`${goal.periodEndDate}T00:00:00`))}` : periodLabels[goal.period] ?? 'All time';
    const formatValue = value => isStudyTimeGoal(goal) ? `${numberFormatter.format(value)} h` : numberFormatter.format(value);
    return <div className="goal-visual"><GoalRing percent={percent}/><div className="goal-visual-main"><div className="goal-meta"><span>{goal.metricDefinition?.name}</span><strong>{formatValue(goal.currentValue)} / {formatValue(goal.targetValue)}</strong></div><div className="goal-progress"><i style={{ width: `${percent}%` }}/></div><div className="goal-insights"><span>{remaining ? `${formatValue(remaining)} remaining` : 'Target reached'}</span><b className={percent >= 100 ? 'complete' : 'on-pace'}>{percent >= 100 ? 'Complete' : `${formatValue(average)} / session`}</b></div><small>{period}</small></div><GoalTrend goal={goal} notes={notes}/></div>;
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

export function SubjectGoals({ subjectId, goals, notes, topics = [], metricDefinitions, onCreateTopic, onRemoveTopic, onCreate, onUpdate, onRemove, onComplete, onSetSubGoalCompletion }) {
  const [open, setOpen] = useState(false);
  const [editingGoal, setEditingGoal] = useState(null);
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

  const resetComposer = () => {
    setOpen(false); setEditingGoal(null); setKind(1); setTitle(''); setTopicId(''); setMetricDefinitionId(''); setTargetValue(''); setTargetDate(''); setPeriod(0); setPeriodStartDate(''); setPeriodEndDate(''); setSubGoals(['']);
  };

  const beginEditing = goal => {
    setEditingGoal(goal); setOpen(true); setKind(goal.kind); setTitle(goal.title); setTopicId(goal.topicId); setMetricDefinitionId(goal.metricDefinition?.id ?? ''); setTargetValue(goal.targetValue?.toString() ?? ''); setTargetDate(goal.targetDate ?? ''); setPeriod(goal.period); setPeriodStartDate(goal.periodStartDate ?? ''); setPeriodEndDate(goal.periodEndDate ?? ''); setSubGoals(goal.subGoals?.map(subGoal => subGoal.title) ?? ['']);
  };

  const beginCreating = () => {
    if (open) resetComposer();
    else resetComposer();
    setOpen(true);
  };

  const save = async event => {
    event.preventDefault();
    const goal = { topicId: topicId || topics[0]?.id || null, title, kind, metricDefinitionId: kind === 1 ? metricDefinitionId || null : null, targetValue: kind === 1 ? parseDecimal(targetValue) : null, targetDate: targetDate || null, period, periodStartDate: period === 4 ? periodStartDate || null : null, periodEndDate: period === 4 ? periodEndDate || null : null, subGoals: kind === 2 ? subGoals.map(subGoal => subGoal.trim()).filter(Boolean) : [] };
    if (!goal.topicId || !title.trim() || (kind === 1 && (!metricDefinitionId || !Number.isFinite(goal.targetValue) || goal.targetValue <= 0)) || (period === 4 && (!periodStartDate || !periodEndDate || periodStartDate > periodEndDate))) return;
    const saved = editingGoal ? await onUpdate(editingGoal.id, { ...goal, title: title.trim() }) : await onCreate({ ...goal, title: title.trim() });
    if (saved) resetComposer();
  };
  return <section className="subject-goals"><div className="subject-goals-head"><div><span>GOALS</span><small>Track progress for this subject.</small></div><button type="button" className="text-button" onClick={beginCreating}><Plus size={15}/> Add goal</button></div>
    {open ? <form className="goal-composer" onSubmit={save}><label>Topic<select value={topicId} onChange={event => setTopicId(event.target.value)} required><option value="">Choose a topic</option>{topics.map(topic => <option key={topic.id} value={topic.id}>{topic.name}</option>)}</select></label><TopicComposer subjectId={subjectId} topics={topics} onCreate={onCreateTopic} onCreated={topic => setTopicId(topic.id)} onRemove={onRemoveTopic} onRemoved={topic => setTopicId(current => current === topic.id ? '' : current)}/><label>Goal title<input value={title} onChange={event => setTitle(event.target.value)} placeholder="What are you aiming for?" maxLength="256"/></label><fieldset><legend>Goal type</legend><label><input type="radio" checked={kind === 1} onChange={() => setKind(1)}/> Metric target</label><label><input type="radio" checked={kind === 2} onChange={() => setKind(2)}/> Completion goal</label></fieldset>{kind === 1 ? <div className="goal-fields"><select value={metricDefinitionId} onChange={event => setMetricDefinitionId(event.target.value)}><option value="">Choose a metric</option>{metricDefinitions.map(definition => <option key={definition.id} value={definition.id}>{definition.name}</option>)}</select><input type="text" inputMode="decimal" value={targetValue} onChange={event => setTargetValue(sanitizeDecimal(event.target.value))} placeholder={metricDefinitions.find(definition => definition.id === metricDefinitionId)?.name?.trim().toUpperCase() === studyTimeMetricName ? 'Target hours' : 'Target value'}/></div> : <><label>Due date (optional)<input type="date" value={targetDate} onChange={event => setTargetDate(event.target.value)}/></label><div className="sub-goal-composer"><span>Sub-goals</span>{subGoals.map((subGoal, index) => <div className="sub-goal-input" key={index}><input value={subGoal} onChange={event => setSubGoals(items => items.map((item, itemIndex) => itemIndex === index ? event.target.value : item))} placeholder={`Step ${index + 1}`}/>{subGoals.length > 1 ? <button type="button" aria-label={`Remove step ${index + 1}`} onClick={() => setSubGoals(items => items.filter((_, itemIndex) => itemIndex !== index))}><Trash2 size={14}/></button> : null}</div>)}</div><button type="button" className="text-button" onClick={() => setSubGoals(items => [...items, ''])}><Plus size={14}/> Add sub-goal</button></>}<label>Period<select value={period} onChange={event => setPeriod(Number(event.target.value))}>{Object.entries(periodLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>{period === 4 ? <div className="goal-fields"><label>Start date<input type="date" value={periodStartDate} onChange={event => setPeriodStartDate(event.target.value)}/></label><label>End date<input type="date" value={periodEndDate} onChange={event => setPeriodEndDate(event.target.value)}/></label></div> : null}<div><button type="button" className="ghost-button" onClick={resetComposer}>Cancel</button><button className="primary-button">{editingGoal ? 'Save changes' : 'Save goal'}</button></div></form> : null}
    <div className="goal-list">{goals.map(goal => <article className="goal-card" key={goal.id}><div className="goal-title"><span>{goal.kind === 1 ? <Target size={15}/> : <CalendarDays size={15}/>}</span><div className="goal-title-copy"><strong>{goal.title}</strong><small>{topics.find(topic => topic.id === goal.topicId)?.name ?? 'Unassigned topic'}</small></div><div className="goal-actions">{goal.kind !== 1 && !goal.subGoals?.length && !goal.isCompleted ? <button type="button" aria-label={`Complete ${goal.title}`} onClick={() => onComplete(goal.id)}><Check size={14}/></button> : null}<button type="button" aria-label={`Edit ${goal.title}`} onClick={() => beginEditing(goal)}><Pencil size={14}/></button><button type="button" aria-label={`Remove ${goal.title}`} onClick={() => onRemove(goal.id)}><Trash2 size={14}/></button></div></div><GoalProgress goal={goal} notes={notes} onSetSubGoalCompletion={onSetSubGoalCompletion}/></article>)}{goals.length === 0 ? <p className="goal-empty">Set a metric or completion goal to see your progress here.</p> : null}</div>
  </section>;
}
