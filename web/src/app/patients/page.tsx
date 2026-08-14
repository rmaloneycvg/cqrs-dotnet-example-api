import { Suspense } from "react";
import { PatientList } from "@/components/features/patient-list";
import { DashboardHeader } from "@/components/dashboard-header";
import { Skeleton } from "@/components/ui/skeleton";

export const metadata = {
  title: "Patients | Clinic Dashboard",
};

export default function PatientsPage() {
  return (
    <div className="flex flex-col min-h-screen">
      <DashboardHeader
        title="Patients"
        description="Manage patient records and clinical data"
      />
      <main className="flex-1 p-4 sm:p-6 lg:p-8">
        <Suspense
          fallback={
            <div className="space-y-4">
              <div className="flex flex-col sm:flex-row gap-3">
                <Skeleton className="h-9 w-full sm:w-64" />
                <Skeleton className="h-9 w-full sm:w-40" />
              </div>
              <Skeleton className="h-[600px] w-full" />
            </div>
          }
        >
          <PatientList />
        </Suspense>
      </main>
    </div>
  );
}
