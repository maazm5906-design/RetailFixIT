import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { Plus, Star, X, Users } from 'lucide-react';
import { vendorsApi } from '../../api/vendors.api';
import { usePermissions } from '../../hooks/usePermissions';

const vendorSchema = z.object({
  name: z.string().min(2, 'Name required'),
  contactEmail: z.string().email('Valid email required'),
  serviceArea: z.string().min(2, 'Service area required'),
  specializations: z.string().optional(),
  capacityLimit: z.number().min(1, 'Capacity must be at least 1').max(100),
});

type VendorForm = z.infer<typeof vendorSchema>;

export default function VendorListPage() {
  const qc = useQueryClient();
  const { can } = usePermissions();
  const [showCreate, setShowCreate] = useState(false);
  const [page, setPage] = useState(1);

  const { data, isLoading } = useQuery({
    queryKey: ['vendors', { page }],
    queryFn: () => vendorsApi.getVendors({ page, pageSize: 20 }),
    placeholderData: (prev) => prev,
  });

  const { register, handleSubmit, reset, formState: { errors } } = useForm<VendorForm>({
    resolver: zodResolver(vendorSchema),
    defaultValues: { capacityLimit: 5 },
  });

  const createMutation = useMutation({
    mutationFn: (data: VendorForm) => vendorsApi.createVendor(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['vendors'] });
      toast.success('Vendor created');
      setShowCreate(false);
      reset();
    },
    onError: () => toast.error('Failed to create vendor'),
  });

  const vendors = data?.items ?? [];
  const totalPages = data?.totalPages ?? 1;

  const field = 'w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500';
  const label = 'block text-sm font-medium text-gray-300 mb-1.5';
  const err = 'mt-1 text-xs text-red-400';

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-white">Vendors</h1>
          <p className="text-gray-400 text-sm mt-1">{data?.totalCount ?? 0} total vendors</p>
        </div>
        {can('manage:vendors') && (
          <button
            onClick={() => setShowCreate(true)}
            className="flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium rounded-lg transition"
          >
            <Plus className="w-4 h-4" />
            Add Vendor
          </button>
        )}
      </div>

      {isLoading ? (
        <div className="flex items-center justify-center py-20">
          <div className="w-8 h-8 border-2 border-blue-600 border-t-transparent rounded-full animate-spin" />
        </div>
      ) : vendors.length === 0 ? (
        <div className="text-center py-20">
          <Users className="w-12 h-12 mx-auto text-gray-700 mb-3" />
          <p className="text-gray-500">No vendors found</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
          {vendors.map((vendor) => (
            <div key={vendor.id} className="bg-gray-900 border border-gray-800 rounded-xl p-5 space-y-3">
              <div className="flex items-start justify-between">
                <div>
                  <h3 className="text-sm font-semibold text-white">{vendor.name}</h3>
                  <p className="text-xs text-gray-500">{vendor.contactEmail}</p>
                </div>
                <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${vendor.isActive ? 'bg-green-900/50 text-green-400 border border-green-800' : 'bg-gray-800 text-gray-500 border border-gray-700'}`}>
                  {vendor.isActive ? 'Active' : 'Inactive'}
                </span>
              </div>

              <div className="flex items-center gap-4 text-xs text-gray-400">
                <span>📍 {vendor.serviceArea}</span>
                {vendor.rating && (
                  <span className="flex items-center gap-0.5 text-yellow-500">
                    <Star className="w-3 h-3 fill-current" />
                    {vendor.rating.toFixed(1)}
                  </span>
                )}
              </div>

              {vendor.specializations && (
                <p className="text-xs text-gray-500">{vendor.specializations}</p>
              )}

              <div className="flex items-center justify-between pt-2 border-t border-gray-800">
                <span className="text-xs text-gray-500">Capacity</span>
                <div className="flex items-center gap-2">
                  <div className="w-24 h-1.5 bg-gray-700 rounded-full overflow-hidden">
                    <div
                      className="h-full bg-blue-500 rounded-full"
                      style={{ width: `${(vendor.currentCapacity / vendor.capacityLimit) * 100}%` }}
                    />
                  </div>
                  <span className="text-xs text-gray-400">{vendor.currentCapacity}/{vendor.capacityLimit}</span>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {totalPages > 1 && (
        <div className="flex items-center justify-center gap-2 text-sm">
          <button
            disabled={page <= 1}
            onClick={() => setPage((p) => p - 1)}
            className="px-3 py-1.5 bg-gray-800 rounded-lg disabled:opacity-40 hover:bg-gray-700 transition text-gray-400"
          >
            Previous
          </button>
          <span className="text-gray-500">Page {page} of {totalPages}</span>
          <button
            disabled={page >= totalPages}
            onClick={() => setPage((p) => p + 1)}
            className="px-3 py-1.5 bg-gray-800 rounded-lg disabled:opacity-40 hover:bg-gray-700 transition text-gray-400"
          >
            Next
          </button>
        </div>
      )}

      {/* Create Vendor Modal */}
      {showCreate && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4">
          <div className="bg-gray-900 rounded-2xl border border-gray-800 w-full max-w-md shadow-2xl">
            <div className="flex items-center justify-between px-6 py-4 border-b border-gray-800">
              <h2 className="text-lg font-semibold text-white">Add Vendor</h2>
              <button onClick={() => setShowCreate(false)} className="p-1 text-gray-400 hover:text-white">
                <X className="w-5 h-5" />
              </button>
            </div>
            <form onSubmit={handleSubmit((d: VendorForm) => createMutation.mutate(d))} className="p-6 space-y-4">
              <div>
                <label className={label}>Vendor Name</label>
                <input {...register('name')} className={field} placeholder="Acme Services" />
                {errors.name && <p className={err}>{errors.name.message}</p>}
              </div>
              <div>
                <label className={label}>Contact Email</label>
                <input {...register('contactEmail')} type="email" className={field} placeholder="contact@vendor.com" />
                {errors.contactEmail && <p className={err}>{errors.contactEmail.message}</p>}
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className={label}>Service Area</label>
                  <input {...register('serviceArea')} className={field} placeholder="Downtown" />
                  {errors.serviceArea && <p className={err}>{errors.serviceArea.message}</p>}
                </div>
                <div>
                  <label className={label}>Max Capacity</label>
                  <input {...register('capacityLimit', { valueAsNumber: true })} type="number" min={1} max={100} className={field} />
                  {errors.capacityLimit && <p className={err}>{errors.capacityLimit.message}</p>}
                </div>
              </div>
              <div>
                <label className={label}>Specializations (optional)</label>
                <input {...register('specializations')} className={field} placeholder="Plumbing, HVAC, Electrical" />
              </div>
              <div className="flex gap-3 pt-2">
                <button
                  type="button"
                  onClick={() => setShowCreate(false)}
                  className="flex-1 py-2.5 bg-gray-800 hover:bg-gray-700 text-white text-sm font-medium rounded-lg transition"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={createMutation.isPending}
                  className="flex-1 py-2.5 bg-blue-600 hover:bg-blue-700 disabled:opacity-60 text-white text-sm font-medium rounded-lg transition"
                >
                  {createMutation.isPending ? 'Adding...' : 'Add Vendor'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
