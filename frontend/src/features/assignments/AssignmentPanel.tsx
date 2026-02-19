import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { UserCheck, Star, Zap, RefreshCw } from 'lucide-react';
import { jobsApi } from '../../api/jobs.api';
import { vendorsApi } from '../../api/vendors.api';
import { usePermissions } from '../../hooks/usePermissions';
import type { Job, Vendor } from '../../types';

interface Props {
  job: Job;
}

export default function AssignmentPanel({ job }: Props) {
  const qc = useQueryClient();
  const { can } = usePermissions();
  const [selectedVendorId, setSelectedVendorId] = useState('');
  const [notes, setNotes] = useState('');
  const [changingVendor, setChangingVendor] = useState(false);

  // isAssigned: use activeAssignment if available, fall back to job.status
  const isAssigned = !!job.activeAssignment
    || job.status === 'Assigned'
    || job.status === 'InProgress';

  const canAssign = can('assign:vendor');
  const showVendorPicker = canAssign && (!isAssigned || changingVendor);

  const { data: vendorsPage } = useQuery({
    queryKey: ['vendors', { hasCapacity: true, pageSize: 50 }],
    queryFn: () => vendorsApi.getVendors({ hasCapacity: true, pageSize: 50 }),
    enabled: showVendorPicker,
  });

  const { data: recommendations } = useQuery({
    queryKey: ['recommendations', job.id],
    queryFn: () => jobsApi.getRecommendations(job.id),
  });

  const vendors = vendorsPage?.items ?? [];
  const latestRec = recommendations?.find((r) => r.status === 'Completed');
  const recVendorIds = new Set(latestRec?.recommendedVendorIds ?? []);

  const assignMutation = useMutation({
    mutationFn: () => jobsApi.assignVendor(job.id, { vendorId: selectedVendorId, notes: notes || undefined }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['job', job.id] });
      qc.invalidateQueries({ queryKey: ['jobs'] });
      toast.success('Vendor assigned successfully');
      setSelectedVendorId('');
      setNotes('');
      setChangingVendor(false);
    },
    onError: () => toast.error('Failed to assign vendor'),
  });

  const requestAIMutation = useMutation({
    mutationFn: () => jobsApi.requestRecommendation(job.id),
    onSuccess: () => toast.success('AI recommendation requested — results will appear shortly'),
    onError: () => toast.error('Failed to request AI recommendation'),
  });

  const revokeMutation = useMutation({
    mutationFn: (assignmentId: string) => jobsApi.revokeAssignment(job.id, assignmentId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['job', job.id] });
      qc.invalidateQueries({ queryKey: ['jobs'] });
      toast.success('Assignment revoked');
    },
    onError: () => toast.error('Failed to revoke assignment'),
  });

  return (
    <div className="space-y-6">
      {/* Current Assignment */}
      {job.activeAssignment && (
        <div className="bg-blue-950/30 border border-blue-800/50 rounded-xl p-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <UserCheck className="w-5 h-5 text-blue-400" />
              <div>
                <p className="text-sm font-medium text-white">Assigned to {job.activeAssignment.vendorName}</p>
                <p className="text-xs text-gray-400">
                  {new Date(job.activeAssignment.assignedAt).toLocaleString()}
                </p>
              </div>
            </div>
            {canAssign && (
              <div className="flex items-center gap-2">
                {!changingVendor && (
                  <button
                    onClick={() => { setChangingVendor(true); setSelectedVendorId(''); setNotes(''); }}
                    className="flex items-center gap-1.5 px-3 py-1.5 bg-blue-900/50 hover:bg-blue-800 text-blue-300 text-xs rounded-lg transition"
                  >
                    <RefreshCw className="w-3 h-3" />
                    Change
                  </button>
                )}
                <button
                  onClick={() => revokeMutation.mutate(job.activeAssignment!.id)}
                  disabled={revokeMutation.isPending}
                  className="px-3 py-1.5 bg-red-900/50 hover:bg-red-800 text-red-300 text-xs rounded-lg transition"
                >
                  Revoke
                </button>
              </div>
            )}
          </div>
        </div>
      )}

      {/* AI Recommendation */}
      {latestRec && (
        <div className="bg-purple-950/30 border border-purple-800/50 rounded-xl p-4">
          <div className="flex items-center gap-2 mb-3">
            <Zap className="w-4 h-4 text-purple-400" />
            <span className="text-sm font-medium text-purple-300">AI Recommendation</span>
            <span className="text-xs text-gray-500 ml-auto">{latestRec.aiProvider} · {latestRec.latencyMs}ms</span>
          </div>
          {latestRec.jobSummary && (
            <p className="text-xs text-gray-400 mb-3">{latestRec.jobSummary}</p>
          )}
          {latestRec.reasoning && (
            <p className="text-xs text-gray-400">{latestRec.reasoning}</p>
          )}
        </div>
      )}

      {/* Vendor Picker — shown when unassigned OR when changing vendor */}
      {canAssign && (!isAssigned || changingVendor) && (
        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-medium text-gray-300">
              {changingVendor ? 'Select a Different Vendor' : 'Assign a Vendor'}
            </h3>
            <div className="flex items-center gap-2">
              {changingVendor && (
                <button
                  onClick={() => { setChangingVendor(false); setSelectedVendorId(''); setNotes(''); }}
                  className="text-xs text-gray-500 hover:text-white transition"
                >
                  Cancel
                </button>
              )}
              {can('request:ai') && (
                <button
                  onClick={() => requestAIMutation.mutate()}
                  disabled={requestAIMutation.isPending}
                  className="flex items-center gap-1.5 px-3 py-1.5 bg-purple-900/40 hover:bg-purple-800/50 text-purple-300 text-xs rounded-lg transition"
                >
                  <Zap className="w-3 h-3" />
                  {requestAIMutation.isPending ? 'Requesting...' : 'AI Recommend'}
                </button>
              )}
            </div>
          </div>

          <div className="space-y-2 max-h-60 overflow-y-auto">
            {vendors.map((vendor: Vendor) => {
              const isRecommended = recVendorIds.has(vendor.id);
              const isCurrent = job.activeAssignment?.vendorId === vendor.id;
              return (
                <label
                  key={vendor.id}
                  className={`flex items-center gap-3 p-3 rounded-lg border cursor-pointer transition ${
                    selectedVendorId === vendor.id
                      ? 'border-blue-600 bg-blue-950/40'
                      : isRecommended
                      ? 'border-purple-700/50 bg-purple-950/20 hover:border-purple-600'
                      : 'border-gray-800 bg-gray-800/50 hover:border-gray-700'
                  }`}
                >
                  <input
                    type="radio"
                    name="vendor"
                    value={vendor.id}
                    checked={selectedVendorId === vendor.id}
                    onChange={() => setSelectedVendorId(vendor.id)}
                    className="sr-only"
                  />
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2">
                      <span className="text-sm font-medium text-white">{vendor.name}</span>
                      {isRecommended && <Zap className="w-3 h-3 text-purple-400" />}
                      {isCurrent && <span className="text-xs text-blue-400 bg-blue-900/40 px-1.5 py-0.5 rounded">Current</span>}
                    </div>
                    <div className="flex items-center gap-3 mt-0.5">
                      <span className="text-xs text-gray-500">{vendor.serviceArea}</span>
                      {vendor.rating && (
                        <span className="flex items-center gap-0.5 text-xs text-yellow-500">
                          <Star className="w-3 h-3 fill-current" />
                          {vendor.rating.toFixed(1)}
                        </span>
                      )}
                      <span className="text-xs text-gray-500">{vendor.availableCapacity} slots</span>
                    </div>
                  </div>
                </label>
              );
            })}
            {vendors.length === 0 && (
              <p className="text-sm text-gray-500 text-center py-4">No vendors with available capacity</p>
            )}
          </div>

          <textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            placeholder="Assignment notes (optional)..."
            rows={2}
            className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
          />

          <button
            disabled={!selectedVendorId || assignMutation.isPending}
            onClick={() => assignMutation.mutate()}
            className="w-full py-2.5 bg-blue-600 hover:bg-blue-700 disabled:opacity-40 text-white text-sm font-medium rounded-lg transition"
          >
            {assignMutation.isPending
              ? 'Assigning...'
              : changingVendor
              ? 'Confirm New Vendor'
              : 'Confirm Assignment'}
          </button>
        </div>
      )}
    </div>
  );
}
