export const PALETTE = ['teal', 'blue', 'amber', 'purple'];

export const initialSubjects = [
  { id: 'learning', name: 'Learning science', color: 'teal', x: 120, y: 105 },
  { id: 'design', name: 'Product design', color: 'blue', x: 470, y: 95 },
  { id: 'methods', name: 'Research methods', color: 'amber', x: 285, y: 335 },
  { id: 'thinking', name: 'Critical thinking', color: 'purple', x: 670, y: 320 },
];

export const initialNotes = [
  { id: 1, subjectId: 'learning', title: 'Spaced repetition', excerpt: 'Reviewing material at expanding intervals strengthens long-term recall.', date: 'Aug 7' },
  { id: 2, subjectId: 'learning', title: 'The testing effect', excerpt: 'Retrieval practice is more effective than simply rereading material.', date: 'Aug 6' },
  { id: 3, subjectId: 'design', title: 'Progressive disclosure', excerpt: 'Show people what they need now and reveal complexity as it becomes useful.', date: 'Aug 5' },
  { id: 4, subjectId: 'design', title: 'Interface as conversation', excerpt: 'Every control should clearly signal what action it makes possible.', date: 'Aug 3' },
  { id: 5, subjectId: 'methods', title: 'Triangulating evidence', excerpt: 'Combine multiple methods to reduce the blind spots of any single source.', date: 'Aug 2' },
];

export const initialConnections = [
  { id: 'learning-methods', source: 'learning', target: 'methods' },
  { id: 'learning-design', source: 'learning', target: 'design' },
  { id: 'design-thinking', source: 'design', target: 'thinking' },
];
