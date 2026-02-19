export type JobStatus = 'New' | 'InReview' | 'Assigned' | 'InProgress' | 'Completed' | 'Cancelled';
export type JobPriority = 'Low' | 'Medium' | 'High' | 'Critical';
export type AssignmentStatus = 'Active' | 'Completed' | 'Revoked';
export type AIRecommendationStatus = 'Pending' | 'Completed' | 'Failed';
export type UserRole = 'Dispatcher' | 'VendorManager' | 'Admin' | 'SupportAgent';

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export interface UserInfo {
  userId: string;
  email: string;
  displayName: string;
  role: UserRole;
  tenantId: string;
}

export interface JobSummary {
  id: string;
  jobNumber: string;
  title: string;
  customerName: string;
  serviceType: string;
  status: JobStatus;
  priority: JobPriority;
  scheduledAt: string | null;
  createdAt: string;
  assignedVendorName: string | null;
}

export interface AssignmentSummary {
  id: string;
  vendorId: string;
  vendorName: string;
  assignedByEmail: string;
  status: AssignmentStatus;
  notes: string | null;
  assignedAt: string;
}

export interface Job {
  id: string;
  jobNumber: string;
  title: string;
  description: string;
  customerName: string;
  serviceAddress: string;
  serviceType: string;
  status: JobStatus;
  priority: JobPriority;
  scheduledAt: string | null;
  completedAt: string | null;
  cancelledAt: string | null;
  createdAt: string;
  updatedAt: string | null;
  activeAssignment: AssignmentSummary | null;
}

export interface Vendor {
  id: string;
  name: string;
  contactEmail: string;
  serviceArea: string;
  specializations: string | null;
  capacityLimit: number;
  currentCapacity: number;
  availableCapacity: number;
  rating: number | null;
  isActive: boolean;
}

export interface Assignment {
  id: string;
  jobId: string;
  vendorId: string;
  vendorName: string;
  assignedByUserId: string;
  assignedByEmail: string;
  status: AssignmentStatus;
  notes: string | null;
  assignedAt: string;
  revokedAt: string | null;
}

export interface AIRecommendation {
  id: string;
  jobId: string;
  status: AIRecommendationStatus;
  promptSummary: string | null;
  recommendedVendorIds: string[];
  reasoning: string | null;
  jobSummary: string | null;
  errorMessage: string | null;
  aiProvider: string | null;
  modelVersion: string | null;
  latencyMs: number | null;
  requestedAt: string;
  completedAt: string | null;
}

export interface AuditLog {
  id: string; // Cosmos DB uses Guid PKs
  entityName: string;
  entityId: string;
  action: string;
  changedByEmail: string;
  oldValues: Record<string, unknown> | null;
  newValues: Record<string, unknown> | null;
  occurredAt: string;
}

export interface CreateJobRequest {
  title: string;
  description: string;
  customerName: string;
  serviceAddress: string;
  serviceType: string;
  priority: JobPriority;
  scheduledAt?: string;
}

export interface UpdateJobRequest {
  title: string;
  description: string;
  customerName: string;
  customerEmail?: string;
  customerPhone?: string;
  serviceAddress: string;
  serviceType: string;
  priority: JobPriority;
  scheduledAt?: string;
}

export interface AssignVendorRequest {
  vendorId: string;
  notes?: string;
}

export interface LoginPayload {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
}
