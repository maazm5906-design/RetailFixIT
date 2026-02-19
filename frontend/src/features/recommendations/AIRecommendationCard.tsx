import React from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Zap, RefreshCw, Clock, CheckCircle, XCircle } from 'lucide-react';
import toast from 'react-hot-toast';
import { jobsApi } from '../../api/jobs.api';
import { usePermissions } from '../../hooks/usePermissions';
import type { AIRecommendation } from '../../types';

interface Props {
  jobId: string;
}

function StatusIcon({ status }: { status: AIRecommendation['status'] }) {
  if (status === 'Completed') return <CheckCircle className="w-4 h-4 text-green-400" />;
  if (status === 'Failed') return <XCircle className="w-4 h-4 text-red-400" />;
  return <Clock className="w-4 h-4 text-yellow-400 animate-pulse" />;
}

export default function AIRecommendationCard({ jobId }: Props) {
  const qc = useQueryClient();
  const { can } = usePermissions();

  const { data: recommendations, isLoading } = useQuery({
    queryKey: ['recommendations', jobId],
    queryFn: () => jobsApi.getRecommendations(jobId),
    refetchInterval: (query) => {
      const data = query.state.data;
      if (Array.isArray(data) && data.some((r) => r.status === 'Pending')) return 3000;
      return false;
    },
  });

  const requestMutation = useMutation({
    mutationFn: () => jobsApi.requestRecommendation(jobId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['recommendations', jobId] });
      toast.success('AI analysis started');
    },
    onError: () => toast.error('Failed to request AI recommendation'),
  });

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <div className="w-6 h-6 border-2 border-purple-600 border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  const latest = recommendations?.[0];

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Zap className="w-5 h-5 text-purple-400" />
          <h3 className="text-sm font-medium text-white">AI Recommendations</h3>
        </div>
        {can('request:ai') && (
          <button
            onClick={() => requestMutation.mutate()}
            disabled={requestMutation.isPending || latest?.status === 'Pending'}
            className="flex items-center gap-1.5 px-3 py-1.5 bg-purple-900/40 hover:bg-purple-800/60 disabled:opacity-50 text-purple-300 text-xs rounded-lg transition"
          >
            <RefreshCw className={`w-3 h-3 ${requestMutation.isPending ? 'animate-spin' : ''}`} />
            {requestMutation.isPending ? 'Requesting...' : 'New Analysis'}
          </button>
        )}
      </div>

      {!latest ? (
        <div className="text-center py-8 text-gray-500 text-sm">
          <Zap className="w-8 h-8 mx-auto mb-2 opacity-30" />
          <p>No AI analysis yet.</p>
          {can('request:ai') && (
            <button
              onClick={() => requestMutation.mutate()}
              className="mt-3 px-4 py-2 bg-purple-700 hover:bg-purple-600 text-white text-xs rounded-lg transition"
            >
              Request AI Analysis
            </button>
          )}
        </div>
      ) : (
        <div className="bg-gray-800/50 border border-gray-700 rounded-xl p-4 space-y-3">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <StatusIcon status={latest.status} />
              <span className="text-xs text-gray-400 capitalize">{latest.status}</span>
            </div>
            <div className="text-right">
              {latest.aiProvider && <span className="text-xs text-gray-500">{latest.aiProvider}</span>}
              {latest.latencyMs && <span className="text-xs text-gray-600 ml-2">{latest.latencyMs}ms</span>}
            </div>
          </div>

          {latest.status === 'Pending' && (
            <div className="flex items-center gap-2 text-yellow-400 text-sm">
              <div className="w-4 h-4 border-2 border-yellow-400 border-t-transparent rounded-full animate-spin" />
              Generating recommendations...
            </div>
          )}

          {latest.status === 'Completed' && (
            <>
              {latest.jobSummary && (
                <div>
                  <p className="text-xs font-medium text-gray-400 mb-1">Job Summary</p>
                  <p className="text-sm text-gray-300">{latest.jobSummary}</p>
                </div>
              )}
              {latest.reasoning && (
                <div>
                  <p className="text-xs font-medium text-gray-400 mb-1">Reasoning</p>
                  <p className="text-sm text-gray-300">{latest.reasoning}</p>
                </div>
              )}
              {latest.recommendedVendorIds.length > 0 && (
                <div>
                  <p className="text-xs font-medium text-gray-400 mb-1">Recommended Vendors</p>
                  <p className="text-xs text-purple-400">{latest.recommendedVendorIds.length} vendor(s) identified — see Assignment panel</p>
                </div>
              )}
            </>
          )}

          {latest.status === 'Failed' && (
            <div className="space-y-1">
              <p className="text-sm text-red-400 font-medium">AI analysis failed.</p>
              {latest.errorMessage && (
                <p className="text-xs text-red-300/70 bg-red-950/40 rounded-lg px-3 py-2 break-words">
                  {latest.errorMessage}
                </p>
              )}
              {can('request:ai') && (
                <p className="text-xs text-gray-500 mt-1">Try requesting a new analysis above.</p>
              )}
            </div>
          )}

          <p className="text-xs text-gray-600">
            {new Date(latest.requestedAt).toLocaleString()}
          </p>
        </div>
      )}
    </div>
  );
}
