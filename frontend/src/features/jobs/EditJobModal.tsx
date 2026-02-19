import React, { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { X } from 'lucide-react';
import toast from 'react-hot-toast';
import { jobsApi } from '../../api/jobs.api';
import type { Job } from '../../types';

const schema = z.object({
  title: z.string().min(1, 'Title is required').max(300),
  description: z.string(),
  customerName: z.string().min(1, 'Customer name is required'),
  customerEmail: z.string().email('Invalid email').or(z.literal('')).optional(),
  customerPhone: z.string().optional(),
  serviceAddress: z.string().min(1, 'Service address is required'),
  serviceType: z.string().min(1, 'Service type is required'),
  priority: z.enum(['Low', 'Medium', 'High', 'Critical']),
  scheduledAt: z.string().optional(),
});

type FormValues = z.infer<typeof schema>;

interface Props {
  job: Job;
  onClose: () => void;
}

export default function EditJobModal({ job, onClose }: Props) {
  const qc = useQueryClient();

  const { register, handleSubmit, formState: { errors }, reset } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      title: job.title,
      description: job.description ?? '',
      customerName: job.customerName,
      serviceAddress: job.serviceAddress,
      serviceType: job.serviceType,
      priority: job.priority,
      scheduledAt: job.scheduledAt ? job.scheduledAt.slice(0, 16) : '',
    },
  });

  useEffect(() => {
    reset({
      title: job.title,
      description: job.description ?? '',
      customerName: job.customerName,
      serviceAddress: job.serviceAddress,
      serviceType: job.serviceType,
      priority: job.priority,
      scheduledAt: job.scheduledAt ? job.scheduledAt.slice(0, 16) : '',
    });
  }, [job, reset]);

  const mutation = useMutation({
    mutationFn: (values: FormValues) =>
      jobsApi.updateJob(job.id, {
        ...values,
        description: values.description ?? '',
        customerEmail: values.customerEmail || undefined,
        scheduledAt: values.scheduledAt || undefined,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['job', job.id] });
      qc.invalidateQueries({ queryKey: ['jobs'] });
      toast.success('Job updated successfully');
      onClose();
    },
    onError: () => toast.error('Failed to update job'),
  });

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
      <div className="bg-gray-900 border border-gray-800 rounded-2xl w-full max-w-lg shadow-2xl">
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-800">
          <h2 className="text-lg font-semibold text-white">Edit Job</h2>
          <button onClick={onClose} className="text-gray-400 hover:text-white transition">
            <X className="w-5 h-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit((v) => mutation.mutate(v))} className="p-6 space-y-4 max-h-[70vh] overflow-y-auto">
          <div>
            <label className="block text-xs text-gray-400 mb-1">Title *</label>
            <input
              {...register('title')}
              className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
            {errors.title && <p className="text-xs text-red-400 mt-1">{errors.title.message}</p>}
          </div>

          <div>
            <label className="block text-xs text-gray-400 mb-1">Description</label>
            <textarea
              {...register('description')}
              rows={3}
              className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-xs text-gray-400 mb-1">Customer Name *</label>
              <input
                {...register('customerName')}
                className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
              {errors.customerName && <p className="text-xs text-red-400 mt-1">{errors.customerName.message}</p>}
            </div>

            <div>
              <label className="block text-xs text-gray-400 mb-1">Customer Email</label>
              <input
                {...register('customerEmail')}
                type="email"
                className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>

          <div>
            <label className="block text-xs text-gray-400 mb-1">Service Address *</label>
            <input
              {...register('serviceAddress')}
              className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
            {errors.serviceAddress && <p className="text-xs text-red-400 mt-1">{errors.serviceAddress.message}</p>}
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-xs text-gray-400 mb-1">Service Type *</label>
              <input
                {...register('serviceType')}
                className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
              {errors.serviceType && <p className="text-xs text-red-400 mt-1">{errors.serviceType.message}</p>}
            </div>

            <div>
              <label className="block text-xs text-gray-400 mb-1">Priority *</label>
              <select
                {...register('priority')}
                className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                <option value="Low">Low</option>
                <option value="Medium">Medium</option>
                <option value="High">High</option>
                <option value="Critical">Critical</option>
              </select>
            </div>
          </div>

          <div>
            <label className="block text-xs text-gray-400 mb-1">Scheduled At</label>
            <input
              {...register('scheduledAt')}
              type="datetime-local"
              className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <div className="flex justify-end gap-3 pt-2">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 text-sm text-gray-400 hover:text-white transition"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={mutation.isPending}
              className="px-5 py-2 bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white text-sm font-medium rounded-lg transition"
            >
              {mutation.isPending ? 'Saving...' : 'Save Changes'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
