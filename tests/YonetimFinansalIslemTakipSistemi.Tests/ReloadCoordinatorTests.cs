using YonetimFinansalIslemTakipSistemi.Application.Common;

namespace YonetimFinansalIslemTakipSistemi.Tests;

/// <summary>
/// ReloadCoordinator: paylaşılan DbContext'te eşzamanlı sorguyu önleyen kuyruklama mantığı.
/// Testler TaskCompletionSource ile "sürmekte olan işlem" durumunu deterministik kurar.
/// </summary>
public class ReloadCoordinatorTests
{
    [Fact]
    public async Task TekCagri_IslemBirKezCalisir_VeBiter()
    {
        var coord = new ReloadCoordinator();
        var runs = 0;

        await coord.RunAsync(() => { runs++; return Task.CompletedTask; });

        Assert.Equal(1, runs);
        Assert.False(coord.IsRunning);
    }

    [Fact]
    public async Task IslemSurerkenGelenCoklustek_TekTekrarCalisir_Coalesce()
    {
        var coord   = new ReloadCoordinator();
        var runs    = 0;
        var started = new TaskCompletionSource();
        var gate    = new TaskCompletionSource();

        Func<Task> op = async () =>
        {
            runs++;
            if (runs == 1) { started.SetResult(); await gate.Task; } // ilk çalışma askıya alınır
        };

        var t1 = coord.RunAsync(op);   // başlar, runs=1, gate'i bekler
        await started.Task;

        var t2 = coord.RunAsync(op);   // sürüyor → yalnız "bekleyen" işaretlenir
        var t3 = coord.RunAsync(op);   // yine bekleyen (t2 ile birleşir)

        Assert.True(t2.IsCompleted);   // kuyruğa alınan çağrılar hemen döner
        Assert.True(t3.IsCompleted);

        gate.SetResult();              // ilk çalışma biter → tek bir tekrar koşar
        await t1;

        Assert.Equal(2, runs);         // ilk + bir birleşik tekrar (t2/t3 tek tekrara indi)
        Assert.False(coord.IsRunning);
    }

    [Fact]
    public async Task KuyrugaAlinanTekrar_EnSonDurumuUygular()
    {
        var coord   = new ReloadCoordinator();
        var seen    = new List<int>();
        var latest  = 0;
        var started = new TaskCompletionSource();
        var gate    = new TaskCompletionSource();
        var first   = true;

        // Delege çalıştığı andaki "latest" değerini okur (VM'lerdeki güncel filtre/arg deseni).
        Func<Task> op = async () =>
        {
            seen.Add(latest);
            if (first) { first = false; started.SetResult(); await gate.Task; }
        };

        latest = 1;
        var t1 = coord.RunAsync(op);   // seen=[1], bekler
        await started.Task;

        latest = 2;
        _ = coord.RunAsync(op);        // kuyruğa alınır (bilinçli await edilmez)
        latest = 3;
        _ = coord.RunAsync(op);        // en son durum: 3

        gate.SetResult();
        await t1;

        Assert.Equal(new[] { 1, 3 }, seen); // tekrar, aradaki 2'yi atlayıp en son 3'ü uygular
    }

    [Fact]
    public async Task IslemHataFirlatirsa_KilitSifirlanir_SonrakiCagriCalisir()
    {
        var coord = new ReloadCoordinator();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => coord.RunAsync(() => throw new InvalidOperationException("test")));

        Assert.False(coord.IsRunning); // hata sonrası kilit açılmalı

        var runs = 0;
        await coord.RunAsync(() => { runs++; return Task.CompletedTask; });
        Assert.Equal(1, runs);
    }

    [Fact]
    public async Task IsRunning_IslemSirasindaTrue_BitisteFalse()
    {
        var coord   = new ReloadCoordinator();
        var started = new TaskCompletionSource();
        var gate    = new TaskCompletionSource();

        var t = coord.RunAsync(async () => { started.SetResult(); await gate.Task; });
        await started.Task;

        Assert.True(coord.IsRunning);

        gate.SetResult();
        await t;

        Assert.False(coord.IsRunning);
    }

    [Fact]
    public async Task NullIslem_ArgumentNullException()
    {
        var coord = new ReloadCoordinator();
        await Assert.ThrowsAsync<ArgumentNullException>(() => coord.RunAsync(null!));
    }
}
