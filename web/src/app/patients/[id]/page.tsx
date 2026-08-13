import { Suspense } from "react";
import { PatientDetail } from "@/components/features/patient-detail";
import { RealtimeIndicator } from "@/components/features/realtime-indicator";

export default async function PatientDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;

  return (
    <div className="flex flex-col h-full">
      <header className="border-b px-6 py-4 flex items-center justify-between">
        <div className="flex items-center gap-4">
          <a
            href="/patients"
            className="text-blue-600 hover:text-blue-800 text-sm"
          >
            ← Back to Patients
          </a>
          <h1 className="text-2xl font-semibold">Patient Detail</h1>
        </div>
        <RealtimeIndicator />
      </header>
      <main className="flex-1 p-6">
        <Suspense
          fallback={
            <div className="animate-pulse space-y-4">
              <div className="h-8 bg-gray-200 rounded w-48" />
              <div className="h-64 bg-gray-200 rounded" />
            </div>
          }
        >
          <PatientDetail patientId={id} />
        </Suspense>
      </main>
    </div>
  );
}
