function getAncestry(subject, subjectsById) {
  const ancestry = [];
  const visited = new Set();
  let current = subject;

  while (current && !visited.has(current.id)) {
    ancestry.unshift(current.name);
    visited.add(current.id);
    current = current.parentSubjectId ? subjectsById.get(current.parentSubjectId) : null;
  }

  return ancestry;
}

function isDescendantOf(subject, ancestorId, subjectsById) {
  const visited = new Set();
  let parentId = subject.parentSubjectId;

  while (parentId && !visited.has(parentId)) {
    if (parentId === ancestorId) return true;
    visited.add(parentId);
    parentId = subjectsById.get(parentId)?.parentSubjectId;
  }

  return false;
}

export function getSubjectParentOptions(subjects, excludedSubjectId = null) {
  const subjectsById = new Map(subjects.map(subject => [subject.id, subject]));

  return subjects
    .filter(subject => subject.id !== excludedSubjectId)
    .filter(subject => !excludedSubjectId || !isDescendantOf(subject, excludedSubjectId, subjectsById))
    .map(subject => {
      const ancestry = getAncestry(subject, subjectsById);
      return { id: subject.id, label: ancestry.join(' / '), depth: ancestry.length };
    })
    .filter(subject => subject.depth < 4)
    .toSorted((left, right) => left.label.localeCompare(right.label));
}
