"use client";

import { usePatient } from "@/hooks/queries/use-patients";
import { useRealtimeSubscription } from "@/hooks/use-realtime-subscription";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";

interface PatientDetailProps {
  patientId: string;
}

const STATUS_VARIANT: Record<string, "success" | "secondary" | "destructive" | "default"> = {
  Active: "success",
  Inactive: "secondary",
  Deceased: "destructive",
  Transferred: "default",
};

export function PatientDetail({ patientId }: PatientDetailProps) {
  useRealtimeSubscription("Patient", patientId);

  const { data: patient, isLoading, isError, error } = usePatient(patientId);

  if (isLoading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-8 w-48" />
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          <Skeleton className="h-40" />
          <Skeleton className="h-40" />
          <Skeleton className="h-40" />
        </div>
      </div>
    );
  }

  if (isError) {
    return (
      <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-4" role="alert">
        <p className="text-destructive">Failed to load patient: {error.message}</p>
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
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div className="min-w-0">
          <h2 className="text-2xl sm:text-3xl font-bold truncate">{patient.fullName}</h2>
          <p className="text-muted-foreground mt-1 text-sm truncate">ID: {patient.id}</p>
        </div>
        <Badge variant={STATUS_VARIANT[patient.status] ?? "secondary"} className="text-sm px-3 py-1 self-start sm:self-auto shrink-0">
          {patient.status}
        </Badge>
      </div>

      {/* Info Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 sm:gap-6">
        <Card>
          <CardHeader>
            <CardTitle>Contact</CardTitle>
          </CardHeader>
          <CardContent>
            <dl className="space-y-2">
              <InfoRow label="Email" value={patient.email} />
              <InfoRow label="Phone" value={patient.phoneNumber} />
              <InfoRow label="Location" value={`${patient.city}, ${patient.state}`} />
            </dl>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Demographics</CardTitle>
          </CardHeader>
          <CardContent>
            <dl className="space-y-2">
              <InfoRow label="Date of Birth" value={patient.dateOfBirth} />
              <InfoRow label="Status" value={patient.status} />
            </dl>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Record</CardTitle>
          </CardHeader>
          <CardContent>
            <dl className="space-y-2">
              <InfoRow label="Created" value={new Date(patient.createdAt).toLocaleDateString()} />
              <InfoRow label="Last Updated" value={new Date(patient.updatedAt).toLocaleString()} />
            </dl>
          </CardContent>
        </Card>
      </div>

      {/* Placeholder sections for future features */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 sm:gap-6">
        <PlaceholderSection title="Appointments" />
        <PlaceholderSection title="Billing" />
      </div>
    </div>
  );
}

function InfoRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex justify-between text-sm">
      <dt className="text-muted-foreground">{label}</dt>
      <dd className="font-medium">{value}</dd>
    </div>
  );
}

function PlaceholderSection({ title }: { title: string }) {
  return (
    <Card className="border-dashed">
      <CardContent className="flex flex-col items-center justify-center py-8">
        <h3 className="font-semibold text-muted-foreground mb-2">{title}</h3>
        <p className="text-sm text-muted-foreground">Coming soon</p>
      </CardContent>
    </Card>
  );
}
