import { apiClient } from './client';
import type { Vendor, PagedResult } from '../types';

export interface VendorsQueryParams {
  page?: number;
  pageSize?: number;
  isActive?: boolean;
  hasCapacity?: boolean;
}

export interface CreateVendorRequest {
  name: string;
  contactEmail: string;
  serviceArea: string;
  specializations?: string;
  capacityLimit: number;
}

export const vendorsApi = {
  getVendors: async (params: VendorsQueryParams = {}): Promise<PagedResult<Vendor>> => {
    const { data } = await apiClient.get<PagedResult<Vendor>>('/vendors', { params });
    return data;
  },

  getVendor: async (id: string): Promise<Vendor> => {
    const { data } = await apiClient.get<Vendor>(`/vendors/${id}`);
    return data;
  },

  createVendor: async (payload: CreateVendorRequest): Promise<Vendor> => {
    const { data } = await apiClient.post<Vendor>('/vendors', payload);
    return data;
  },

  updateVendor: async (id: string, payload: Partial<CreateVendorRequest>): Promise<Vendor> => {
    const { data } = await apiClient.put<Vendor>(`/vendors/${id}`, payload);
    return data;
  },
};
