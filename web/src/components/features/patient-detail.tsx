"use client";

import { usePatient } from "@/hooks/queries/use-patients";
import { useRealtimeSubscription } from "@/hooks/use-realtime-subscription";

interface PatientDetailProps {
  patientId: string;
}

export function PatientDetail({ patientId }: PatientDetailProps) {
  // Subscribe to real-time updates for this patient
  useRealtimeSubscription("Patient", patientId);

  const { data: patient, isLoading, isError, error } = usePatient(patientId);

  if (isLoading) {
    return (
      <div className="animate-pulse space-y-4">
        <div className="h-8 bg-gray-200 rounded w-48" />
        <div className="h-64 bg-gray-200 rounded" />
      </div>
    );
  }

  if (isError) {
    return (
      <div className="rounded-lg border border-red-200 bg-red-50 p-4" role="alert">
        <p className="text-red-800">Failed to load patient: {error.message}</p>
      </div>
    );
  }

  if (!patient) {
    return (
      <div className="rounded-lg border border-yellow-200 bg-yellow-50 p-4">
        <p className="text-yellow-800">Patient not found.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Patient Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-3xl font-bold">{patient.fullName}</h2>
          <p className="text-gray-500 mt-1">ID: {patient.id}</p>
        </div>
        <StatusBadge status={patient.status} />
      </div>

      {/* Info Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        <InfoCard title="Contact">
          <InfoRow label="Email" value={patient.email} />
          <InfoRow label="Phone" value={patient.phoneNumber} />
          <InfoRow label="Location" value={`${patient.city}, ${patient.state}`} />
        </InfoCard>

        <InfoCard title="Demographics">
          <InfoRow label="Date of Birth" value={patient.dateOfBirth} />
          <InfoRow label="Status" value={patient.status} />
        </InfoCard>

        <InfoCard title="Record">
          <InfoRow
            label="Created"
            value={new Date(patient.createdAt).toLocaleDateString()}
          />
          <InfoRow
            label="Last Updated"
            value={new Date(patient.updatedAt).toLocaleString()}
          />
        </InfoCard>
      </div>

      {/* Placeholder sections for future features */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <PlaceholderSection title="Appointments" />
        <PlaceholderSection title="Billing" />
      </div>
    </div>
  );
}

function InfoCard({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="rounded-lg border p-4">
      <h3 className="font-semibold text-gray-900 mb-3">{title}</h3>
      <dl className="space-y-2">{children}</dl>
    </div>
  );
}

function InfoRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex justify-between text-sm">
      <dt className="text-gray-500">{label}</dt>
      <dd className="text-gray-900 font-medium">{value}</dd>
    </div>
  );
}

function StatusBadge({ status }: { status: string }) {
  const colors: Record<string, string> = {
    Active: "bg-green-100 text-green-800 border-green-200",
    Inactive: "bg-gray-100 text-gray-800 border-gray-200",
    Deceased: "bg-red-100 text-red-800 border-red-200",
    Transferred: "bg-blue-100 text-blue-800 border-blue-200",
  };

  return (
    <span
      className={`inline-flex items-center rounded-full border px-3 py-1 text-sm font-medium ${colors[status] ?? "bg-gray-100 text-gray-800 border-gray-200"}`}
    >
      {status}
    </span>
  );
}

function PlaceholderSection({ title }: { title: string }) {
  return (
    <div className="rounded-lg border border-dashed border-gray-300 p-6 text-center">
      <h3 className="font-semibold text-gray-400 mb-2">{title}</h3>
      <p className="text-sm text-gray-400">Coming soon</p>
    </div>
  );
}
