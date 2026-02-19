import React from 'react';
import type { JobStatus, JobPriority } from '../../types';

const STATUS_STYLES: Record<JobStatus, string> = {
  New: 'bg-slate-800 text-slate-300 border-slate-700',
  InReview: 'bg-yellow-900/50 text-yellow-300 border-yellow-700',
  Assigned: 'bg-blue-900/50 text-blue-300 border-blue-700',
  InProgress: 'bg-purple-900/50 text-purple-300 border-purple-700',
  Completed: 'bg-green-900/50 text-green-300 border-green-700',
  Cancelled: 'bg-gray-800 text-gray-400 border-gray-700',
};

const STATUS_LABELS: Record<JobStatus, string> = {
  New: 'New',
  InReview: 'In Review',
  Assigned: 'Assigned',
  InProgress: 'In Progress',
  Completed: 'Completed',
  Cancelled: 'Cancelled',
};

const PRIORITY_STYLES: Record<JobPriority, string> = {
  Low: 'bg-gray-800 text-gray-400 border-gray-700',
  Medium: 'bg-blue-900/50 text-blue-300 border-blue-700',
  High: 'bg-orange-900/50 text-orange-300 border-orange-700',
  Critical: 'bg-red-900/50 text-red-300 border-red-700',
};

export function StatusBadge({ status }: { status: JobStatus }) {
  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium border ${STATUS_STYLES[status] ?? 'bg-gray-800 text-gray-400 border-gray-700'}`}>
      {STATUS_LABELS[status] ?? status}
    </span>
  );
}

export function PriorityBadge({ priority }: { priority: JobPriority }) {
  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium border ${PRIORITY_STYLES[priority] ?? 'bg-gray-800 text-gray-400 border-gray-700'}`}>
      {priority}
    </span>
  );
}
