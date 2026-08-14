import { Suspense } from "react";
import Link from "next/link";
import { PatientDetail } from "@/components/features/patient-detail";
import { DashboardHeader } from "@/components/dashboard-header";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";

export default async function PatientDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;

  return (
    <div className="flex flex-col min-h-screen">
      <DashboardHeader title="Patient Detail">
        <Link href="/patients">
          <Button variant="ghost" size="sm" className="gap-1.5">
            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 19.5 3 12m0 0 7.5-7.5M3 12h18" />
            </svg>
            <span className="hidden sm:inline">Back to Patients</span>
          </Button>
        </Link>
      </DashboardHeader>
      <main className="flex-1 p-4 sm:p-6 lg:p-8">
        <Suspense
          fallback={
            <div className="space-y-6">
              <div className="flex items-center justify-between">
                <Skeleton className="h-9 w-48" />
                <Skeleton className="h-7 w-20" />
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 sm:gap-6">
                <Skeleton className="h-44" />
                <Skeleton className="h-44" />
                <Skeleton className="h-44" />
              </div>
            </div>
          }
        >
          <PatientDetail patientId={id} />
        </Suspense>
      </main>
    </div>
  );
}
