import React, { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft, MapPin, User, Calendar, Tag, Pencil } from 'lucide-react';
import toast from 'react-hot-toast';
import { jobsApi } from '../../api/jobs.api';
import { StatusBadge, PriorityBadge } from '../../components/ui/Badge';
import AssignmentPanel from '../assignments/AssignmentPanel';
import AIRecommendationCard from '../recommendations/AIRecommendationCard';
import EditJobModal from './EditJobModal';
import { usePermissions } from '../../hooks/usePermissions';

type Tab = 'details' | 'assignment' | 'ai';

export default function JobDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const qc = useQueryClient();
  const { can } = usePermissions();
  const [activeTab, setActiveTab] = useState<Tab>('details');
  const [showEditModal, setShowEditModal] = useState(false);

  const { data: job, isLoading, error } = useQuery({
    queryKey: ['job', id],
    queryFn: () => jobsApi.getJob(id!),
    enabled: !!id,
  });

  const cancelMutation = useMutation({
    mutationFn: () => jobsApi.cancelJob(id!),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['job', id] });
      qc.invalidateQueries({ queryKey: ['jobs'] });
      toast.success('Job cancelled');
    },
    onError: () => toast.error('Failed to cancel job'),
  });

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="w-8 h-8 border-2 border-blue-600 border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  if (error || !job) {
    return (
      <div className="flex items-center justify-center min-h-screen text-gray-400">
        Job not found.
      </div>
    );
  }

  const tabs: { id: Tab; label: string }[] = [
    { id: 'details', label: 'Details' },
    { id: 'assignment', label: 'Assignment' },
    { id: 'ai', label: 'AI Analysis' },
  ];

  return (
    <div className="p-6 max-w-5xl mx-auto space-y-6">
      {/* Back + Header */}
      <div>
        <button
          onClick={() => navigate('/dashboard')}
          className="flex items-center gap-2 text-sm text-gray-400 hover:text-white transition mb-4"
        >
          <ArrowLeft className="w-4 h-4" />
          Back to Dashboard
        </button>

        <div className="flex items-start justify-between gap-4">
          <div>
            <div className="flex items-center gap-3 mb-1">
              <span className="text-xs font-mono text-gray-500">{job.jobNumber}</span>
              <StatusBadge status={job.status} />
              <PriorityBadge priority={job.priority} />
            </div>
            <h1 className="text-2xl font-bold text-white">{job.title}</h1>
          </div>
          <div className="flex items-center gap-2">
            {can('create:job') && job.status !== 'Cancelled' && job.status !== 'Completed' && (
              <button
                onClick={() => setShowEditModal(true)}
                className="flex items-center gap-1.5 px-4 py-2 bg-gray-800 hover:bg-gray-700 text-gray-300 text-sm rounded-lg transition"
              >
                <Pencil className="w-4 h-4" />
                Edit
              </button>
            )}
            {can('cancel:job') && job.status !== 'Cancelled' && job.status !== 'Completed' && (
              <button
                onClick={() => {
                  if (confirm('Are you sure you want to cancel this job?')) {
                    cancelMutation.mutate();
                  }
                }}
                disabled={cancelMutation.isPending}
                className="px-4 py-2 bg-red-900/40 hover:bg-red-800/60 text-red-300 text-sm rounded-lg transition"
              >
                Cancel Job
              </button>
            )}
          </div>
        </div>
      </div>

      {/* Meta info */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {[
          { icon: User, label: 'Customer', value: job.customerName },
          { icon: Tag, label: 'Service Type', value: job.serviceType },
          { icon: MapPin, label: 'Address', value: job.serviceAddress },
          { icon: Calendar, label: 'Scheduled', value: job.scheduledAt ? new Date(job.scheduledAt).toLocaleDateString() : 'Not scheduled' },
        ].map(({ icon: Icon, label, value }) => (
          <div key={label} className="bg-gray-900 border border-gray-800 rounded-xl p-4">
            <div className="flex items-center gap-2 mb-1">
              <Icon className="w-4 h-4 text-gray-500" />
              <span className="text-xs text-gray-500">{label}</span>
            </div>
            <p className="text-sm text-white font-medium truncate">{value}</p>
          </div>
        ))}
      </div>

      {/* Tabs */}
      <div className="border-b border-gray-800">
        <div className="flex gap-1">
          {tabs.map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`px-4 py-2.5 text-sm font-medium transition border-b-2 -mb-px ${
                activeTab === tab.id
                  ? 'border-blue-500 text-white'
                  : 'border-transparent text-gray-400 hover:text-white'
              }`}
            >
              {tab.label}
            </button>
          ))}
        </div>
      </div>

      {/* Tab Content */}
      <div className="bg-gray-900 border border-gray-800 rounded-xl p-6">
        {activeTab === 'details' && (
          <div className="space-y-4">
            <div>
              <h3 className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-2">Description</h3>
              <p className="text-sm text-gray-300 leading-relaxed">{job.description}</p>
            </div>
            <div className="grid grid-cols-2 gap-4 pt-4 border-t border-gray-800">
              <div>
                <span className="text-xs text-gray-500">Created</span>
                <p className="text-sm text-gray-300">{new Date(job.createdAt).toLocaleString()}</p>
              </div>
              {job.updatedAt && (
                <div>
                  <span className="text-xs text-gray-500">Last Updated</span>
                  <p className="text-sm text-gray-300">{new Date(job.updatedAt).toLocaleString()}</p>
                </div>
              )}
              {job.completedAt && (
                <div>
                  <span className="text-xs text-gray-500">Completed</span>
                  <p className="text-sm text-gray-300">{new Date(job.completedAt).toLocaleString()}</p>
                </div>
              )}
              {job.cancelledAt && (
                <div>
                  <span className="text-xs text-gray-500">Cancelled</span>
                  <p className="text-sm text-gray-300">{new Date(job.cancelledAt).toLocaleString()}</p>
                </div>
              )}
            </div>
          </div>
        )}

        {activeTab === 'assignment' && <AssignmentPanel job={job} />}

        {activeTab === 'ai' && <AIRecommendationCard jobId={job.id} />}
      </div>

      {showEditModal && (
        <EditJobModal job={job} onClose={() => setShowEditModal(false)} />
      )}
    </div>
  );
}
