import { Suspense } from "react";
import { PatientList } from "@/components/features/patient-list";
import { RealtimeIndicator } from "@/components/features/realtime-indicator";

export const metadata = {
  title: "Patients | Clinic Dashboard",
};

export default function PatientsPage() {
  return (
    <div className="flex flex-col h-full">
      <header className="border-b px-6 py-4 flex items-center justify-between">
        <h1 className="text-2xl font-semibold">Patients</h1>
        <RealtimeIndicator />
      </header>
      <main className="flex-1 p-6">
        <Suspense
          fallback={
            <div className="animate-pulse space-y-4">
              <div className="h-10 bg-gray-200 rounded w-64" />
              <div className="h-96 bg-gray-200 rounded" />
            </div>
          }
        >
          <PatientList />
        </Suspense>
      </main>
    </div>
  );
}
