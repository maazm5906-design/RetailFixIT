import React from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { X } from 'lucide-react';
import { jobsApi } from '../../api/jobs.api';
import type { JobPriority } from '../../types';

const schema = z.object({
  title: z.string().min(3, 'Title must be at least 3 characters'),
  description: z.string().min(10, 'Description must be at least 10 characters'),
  customerName: z.string().min(2, 'Customer name required'),
  serviceAddress: z.string().min(5, 'Service address required'),
  serviceType: z.string().min(1, 'Service type required'),
  priority: z.enum(['Low', 'Medium', 'High', 'Critical']),
  scheduledAt: z.string().optional(),
});

type FormData = z.infer<typeof schema>;

interface Props {
  onClose: () => void;
}

export default function CreateJobModal({ onClose }: Props) {
  const qc = useQueryClient();

  const { register, handleSubmit, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { priority: 'Medium' },
  });

  const mutation = useMutation({
    mutationFn: (data: FormData) => jobsApi.createJob({
      ...data,
      priority: data.priority as JobPriority,
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['jobs'] });
      toast.success('Job created successfully');
      onClose();
    },
    onError: () => toast.error('Failed to create job'),
  });

  const field = 'w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500';
  const label = 'block text-sm font-medium text-gray-300 mb-1.5';
  const err = 'mt-1 text-xs text-red-400';

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4">
      <div className="bg-gray-900 rounded-2xl border border-gray-800 w-full max-w-lg shadow-2xl">
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-800">
          <h2 className="text-lg font-semibold text-white">Create New Job</h2>
          <button onClick={onClose} className="p-1 text-gray-400 hover:text-white transition">
            <X className="w-5 h-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit((d) => mutation.mutate(d))} className="p-6 space-y-4">
          <div>
            <label className={label}>Job Title</label>
            <input {...register('title')} className={field} placeholder="e.g. HVAC Repair - Unit 4B" />
            {errors.title && <p className={err}>{errors.title.message}</p>}
          </div>

          <div>
            <label className={label}>Description</label>
            <textarea {...register('description')} rows={3} className={field} placeholder="Describe the issue in detail..." />
            {errors.description && <p className={err}>{errors.description.message}</p>}
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className={label}>Customer Name</label>
              <input {...register('customerName')} className={field} placeholder="John Smith" />
              {errors.customerName && <p className={err}>{errors.customerName.message}</p>}
            </div>
            <div>
              <label className={label}>Priority</label>
              <select {...register('priority')} className={field}>
                <option value="Low">Low</option>
                <option value="Medium">Medium</option>
                <option value="High">High</option>
                <option value="Critical">Critical</option>
              </select>
            </div>
          </div>

          <div>
            <label className={label}>Service Address</label>
            <input {...register('serviceAddress')} className={field} placeholder="123 Main St, City, State" />
            {errors.serviceAddress && <p className={err}>{errors.serviceAddress.message}</p>}
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className={label}>Service Type</label>
              <select {...register('serviceType')} className={field}>
                <option value="">Select type</option>
                {['Plumbing', 'Electrical', 'HVAC', 'Appliance', 'General Maintenance', 'Roofing', 'Landscaping'].map((t) => (
                  <option key={t} value={t}>{t}</option>
                ))}
              </select>
              {errors.serviceType && <p className={err}>{errors.serviceType.message}</p>}
            </div>
            <div>
              <label className={label}>Scheduled Date (optional)</label>
              <input {...register('scheduledAt')} type="datetime-local" className={field} />
            </div>
          </div>

          <div className="flex gap-3 pt-2">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 py-2.5 bg-gray-800 hover:bg-gray-700 text-white text-sm font-medium rounded-lg transition"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={mutation.isPending}
              className="flex-1 py-2.5 bg-blue-600 hover:bg-blue-700 disabled:opacity-60 text-white text-sm font-medium rounded-lg transition"
            >
              {mutation.isPending ? 'Creating...' : 'Create Job'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
