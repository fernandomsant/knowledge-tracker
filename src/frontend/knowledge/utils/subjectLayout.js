const NODE_SIZE = 184;
export const NODE_WIDTH = NODE_SIZE;
export const NODE_HEIGHT = NODE_SIZE;
export const CANVAS_WORLD_WIDTH = 3600;
export const CANVAS_WORLD_HEIGHT = 2400;

const INITIAL_LAYOUT_WIDTH = 1200;
const INITIAL_LAYOUT_HEIGHT = 900;

export function layoutSubjects(subjects, connections) {
  const byId = new Map(subjects.map(subject => [subject.id, subject]));
  const childrenByParent = new Map(subjects.map(subject => [subject.id, []]));
  subjects.forEach(subject => {
    if (byId.has(subject.parentSubjectId)) childrenByParent.get(subject.parentSubjectId).push(subject);
  });
  const rootFor = new Map();
  const findRoot = subject => {
    if (rootFor.has(subject.id)) return rootFor.get(subject.id);
    const seen = new Set([subject.id]);
    let current = subject;
    while (current.parentSubjectId && byId.has(current.parentSubjectId) && !seen.has(current.parentSubjectId)) {
      seen.add(current.parentSubjectId);
      current = byId.get(current.parentSubjectId);
    }
    rootFor.set(subject.id, current.id);
    return current.id;
  };
  subjects.forEach(findRoot);

  const relatedRoots = new Map();
  const relatedRootSet = id => {
    if (!relatedRoots.has(id)) relatedRoots.set(id, new Set());
    return relatedRoots.get(id);
  };
  connections.forEach(connection => {
    const sourceRoot = rootFor.get(connection.source);
    const targetRoot = rootFor.get(connection.target);
    if (!sourceRoot || !targetRoot || sourceRoot === targetRoot) return;
    relatedRootSet(sourceRoot).add(targetRoot);
    relatedRootSet(targetRoot).add(sourceRoot);
  });
  const roots = subjects.filter(subject => rootFor.get(subject.id) === subject.id);
  const cluster = new Map();
  let nextCluster = 0;
  roots.forEach(root => {
    if (cluster.has(root.id)) return;
    const stack = [root.id];
    while (stack.length) {
      const id = stack.pop();
      if (cluster.has(id)) continue;
      cluster.set(id, nextCluster);
      relatedRoots.get(id)?.forEach(neighbor => stack.push(neighbor));
    }
    nextCluster += 1;
  });
  const levels = [];
  const visited = new Set();
  const visit = (subject, depth) => {
    if (visited.has(subject.id)) return;
    visited.add(subject.id);
    (levels[depth] ??= []).push(subject);
    childrenByParent.get(subject.id)
      ?.toSorted((left, right) => left.name.localeCompare(right.name))
      .forEach(child => visit(child, depth + 1));
  };
  roots.toSorted((left, right) => (cluster.get(left.id) ?? Number.MAX_SAFE_INTEGER) - (cluster.get(right.id) ?? Number.MAX_SAFE_INTEGER) || left.name.localeCompare(right.name)).forEach(root => visit(root, 0));
  subjects.filter(subject => !visited.has(subject.id)).forEach(subject => visit(subject, 0));
  const positioned = new Map();
  levels.forEach((level, depth) => {
    const gap = Math.max(18, Math.min(68, (INITIAL_LAYOUT_WIDTH - level.length * NODE_WIDTH) / Math.max(1, level.length - 1)));
    const usedWidth = level.length * NODE_WIDTH + Math.max(0, level.length - 1) * gap;
    const startX = Math.max(20, (INITIAL_LAYOUT_WIDTH - usedWidth) / 2);
    const y = Math.min(INITIAL_LAYOUT_HEIGHT - NODE_HEIGHT - 20, 48 + depth * 210);
    level.forEach((subject, index) => positioned.set(subject.id, { ...subject, x: startX + index * (NODE_WIDTH + gap), y }));
  });
  const generatedSubjects = subjects.map(subject => positioned.get(subject.id) ?? { ...subject, x: 20, y: 20 });
  if (!generatedSubjects.length) return generatedSubjects;
  const centroid = generatedSubjects.reduce(
    (total, subject) => ({ x: total.x + subject.x + NODE_WIDTH / 2, y: total.y + subject.y + NODE_HEIGHT / 2 }),
    { x: 0, y: 0 },
  );
  const offsetX = CANVAS_WORLD_WIDTH / 2 - centroid.x / generatedSubjects.length;
  const offsetY = CANVAS_WORLD_HEIGHT / 2 - centroid.y / generatedSubjects.length;

  return generatedSubjects.map(subject => ({
    ...subject,
    x: Math.min(CANVAS_WORLD_WIDTH - NODE_WIDTH, Math.max(0, subject.x + offsetX)),
    y: Math.min(CANVAS_WORLD_HEIGHT - NODE_HEIGHT, Math.max(0, subject.y + offsetY)),
  }));
}
