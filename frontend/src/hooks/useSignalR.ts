import { useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { useQueryClient } from '@tanstack/react-query';
import { useAuthStore } from '../store/authStore';
import { getSignalRConnection } from '../lib/signalr';
import toast from 'react-hot-toast';

export function useSignalR() {
  const user = useAuthStore((s) => s.user);
  const queryClient = useQueryClient();
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    if (!user) return;

    const connection = getSignalRConnection();
    connectionRef.current = connection;

    const handleJobCreated = (payload: { jobId: string; title: string }) => {
      queryClient.invalidateQueries({ queryKey: ['jobs'] });
      toast.success(`New job created: ${payload.title}`);
    };

    const handleJobUpdated = (payload: { jobId: string }) => {
      queryClient.invalidateQueries({ queryKey: ['jobs'] });
      queryClient.invalidateQueries({ queryKey: ['job', payload.jobId] });
    };

    const handleJobAssigned = (payload: { jobId: string }) => {
      queryClient.invalidateQueries({ queryKey: ['jobs'] });
      queryClient.invalidateQueries({ queryKey: ['job', payload.jobId] });
      queryClient.invalidateQueries({ queryKey: ['assignments', payload.jobId] });
      toast.success('Job assigned to vendor');
    };

    const handleAIRecommendationReady = (payload: { jobId: string }) => {
      queryClient.invalidateQueries({ queryKey: ['recommendations', payload.jobId] });
      toast.success('AI recommendations are ready', { icon: '🤖' });
    };

    connection.on('JobCreated', handleJobCreated);
    connection.on('JobUpdated', handleJobUpdated);
    connection.on('JobAssigned', handleJobAssigned);
    connection.on('AIRecommendationReady', handleAIRecommendationReady);

    if (connection.state === signalR.HubConnectionState.Disconnected) {
      connection
        .start()
        .then(() => {
          // Join the tenant group so the server can broadcast events to this client
          if (user.tenantId) {
            connection.invoke('JoinTenantGroup', user.tenantId.toString()).catch(console.error);
          }
        })
        .catch((err) => console.error('SignalR connection error:', err));
    }

    return () => {
      connection.off('JobCreated', handleJobCreated);
      connection.off('JobUpdated', handleJobUpdated);
      connection.off('JobAssigned', handleJobAssigned);
      connection.off('AIRecommendationReady', handleAIRecommendationReady);
    };
  }, [user, queryClient]);

  return connectionRef.current;
}