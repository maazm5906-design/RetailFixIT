import { apiClient } from './client';
import type {
  Job,
  JobSummary,
  PagedResult,
  CreateJobRequest,
  UpdateJobRequest,
  Assignment,
  AssignVendorRequest,
  AIRecommendation,
  LoginPayload,
  LoginResponse,
  AuditLog,
  JobStatus,
} from '../types';

export interface JobsQueryParams {
  page?: number;
  pageSize?: number;
  status?: string;
  priority?: string;
  search?: string;
  sortBy?: string;
}

export const jobsApi = {
  getJobs: async (params: JobsQueryParams = {}): Promise<PagedResult<JobSummary>> => {
    const { data } = await apiClient.get<PagedResult<JobSummary>>('/jobs', { params });
    return data;
  },

  getJob: async (id: string): Promise<Job> => {
    const { data } = await apiClient.get<Job>(`/jobs/${id}`);
    return data;
  },

  createJob: async (payload: CreateJobRequest): Promise<Job> => {
    const { data } = await apiClient.post<Job>('/jobs', payload);
    return data;
  },

  updateJob: async (id: string, payload: UpdateJobRequest): Promise<Job> => {
    const { data } = await apiClient.put<Job>(`/jobs/${id}`, payload);
    return data;
  },

  updateJobStatus: async (id: string, status: JobStatus): Promise<void> => {
    await apiClient.patch(`/jobs/${id}/status`, { status });
  },

  cancelJob: async (id: string): Promise<void> => {
    await apiClient.delete(`/jobs/${id}/cancel`);
  },

  getAssignments: async (jobId: string): Promise<Assignment[]> => {
    const { data } = await apiClient.get<Assignment[]>(`/jobs/${jobId}/assignments`);
    return data;
  },

  assignVendor: async (jobId: string, payload: AssignVendorRequest): Promise<Assignment> => {
    const { data } = await apiClient.post<Assignment>(`/jobs/${jobId}/assignments`, payload);
    return data;
  },

  revokeAssignment: async (jobId: string, assignmentId: string): Promise<void> => {
    await apiClient.delete(`/jobs/${jobId}/assignments/${assignmentId}`);
  },

  requestRecommendation: async (jobId: string): Promise<void> => {
    await apiClient.post(`/jobs/${jobId}/recommendations`);
  },

  getRecommendations: async (jobId: string): Promise<AIRecommendation[]> => {
    const { data } = await apiClient.get<AIRecommendation[]>(`/jobs/${jobId}/recommendations`);
    return data;
  },

  getAuditLogs: async (): Promise<AuditLog[]> => {
    const { data } = await apiClient.get<AuditLog[]>('/audit-logs');
    return data;
  },

  login: async (payload: LoginPayload): Promise<LoginResponse> => {
    const { data } = await apiClient.post('/auth/login', payload);
    return {
      token: data.accessToken ?? data.token,
      expiresAt: data.expiresAt ?? '',
    };
  },
};
